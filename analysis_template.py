import numpy as np
import random
from mpdaf.obj import Cube
from scipy import ndimage


#------------------- Constants and Globals ---------------------------------------------
lambda_0 = 6562.8       #H_alpha
delta = 18              #Breite des Filters
c = 299792.458          # km/s
theta = 0.2             # arcsec/pixel
d = 897                 # parsec
t_sn_seconds = 56 * 365.25 * 24 * 3600
flux_threshold = 150    # Threshold for flux
aperture_radius = 12    # in Pixeln
sn_threshold=2          # S/N threshold
discard_fraction=0.1    #discard fraction for flux
min_neighbors=3         # Minimum number of neighboring spaxels for S/N threshold


#------------------- Paths ---------------------------------------------
output_path = "path/to/your/unityproject/Assets/Data/H_alpha.txt"
cube_path = "path/to/your/HR_DEL_reduced.fits"
reduced_cube = Cube(cube_path) #read in background subtracted cube


#---------------- H_Alpha Filter ---------------------------------------------
cube = reduced_cube.select_lambda(lambda_0 - delta, lambda_0 + delta)
depth_3d = cube[:, 30, 30].data.shape[0]



#------------------- Zentrum im Cube bestimmen und Binary Maskieren ---------------------------------------------
def get_star_coord(cube, n):
    data = cube.sum(axis=0).data
    indices = np.argsort(data.ravel())[-n:]
    yx_coords = np.unravel_index(indices, data.shape)
    return [(y, x) for y, x in zip(*yx_coords)]

center_list = list(reversed(get_star_coord(reduced_cube, 5)))
y_center = center_list[0][0] 
x_center = center_list[0][1]

def mask_star(cube, star_position, aperture_radius):
    masked_cube = cube.copy()  # Kopie des Cubes erstellen, um das Original nicht zu verändern
    y, x = np.ogrid[:cube.shape[1], :cube.shape[2]]  # Koordinatengitter erstellen

    # Entfernungsquadrat vom Sternzentrum berechnen
    distance_sq = (x - star_position[1])**2 + (y - star_position[0])**2

    # Maske erstellen, die True innerhalb der Apertur ist
    mask = distance_sq <= (aperture_radius**2)

    # Alle Spektren innerhalb der Apertur maskieren
    for i in range(cube.shape[0]):  # Über jede Schicht des Cubes iterieren
        masked_cube.data[i, mask] = np.nan  # NaN setzen, um die Sternregion zu maskieren

    return masked_cube



#------------------- Extract Nova Shell ---------------------------------------------
def extract_nova_shell(cube):

    # S/N für jeden Spaxel im Subcube
    sn_map = abs(cube.data / np.sqrt(cube.var))

    # Erste Auswahlkriterium: S/N-Schwelle
    signal_mask = sn_map > sn_threshold

    # Zweites Kriterium: Mindestanzahl benachbarter Spaxels
    neighbor_mask = ndimage.generic_filter(signal_mask.astype(int), np.sum, size=3, mode='constant', cval=0) >= min_neighbors

    # Kombiniere beide Kriterien
    final_mask = signal_mask & neighbor_mask

    
    sorted_flux = np.sort(np.abs(cube.data[final_mask]))
    cutoff_index = int(discard_fraction * len(sorted_flux))
    flux_threshold = sorted_flux[cutoff_index]
    # Anwenden des Schwellenwerts auf die ursprünglichen Daten
    final_mask &= np.abs(cube.data) >= flux_threshold



    # Anwenden der endgültigen Maske
    filtered_cube = cube.copy()
    filtered_cube.data[~final_mask] = 0  # Null setzen, wo die Maske nicht erfüllt ist

    return filtered_cube



#------------------- Calculate Dopplershift and R_z ---------------------------------------------
def calculate_velocity_fields(cube):
    doppler_shifts_velocity = np.zeros((cube.shape[1], cube.shape[2], depth_3d))
    Rz = np.zeros((cube.shape[1], cube.shape[2], depth_3d))
    flux_grid = np.zeros((cube.shape[1], cube.shape[2], depth_3d))
    for i in range(cube.shape[1]):
        for j in range(cube.shape[2]):
            spec = cube[:, i, j]
            wavelengths = spec.wave.coord()
            doppler_shifts = c * ((wavelengths - lambda_0) / lambda_0)
            doppler_shifts_velocity[i, j, :] = doppler_shifts
            Rz[i, j, :] = doppler_shifts * t_sn_seconds
            flux_grid[i, j, :] = spec.data
    return doppler_shifts_velocity, flux_grid, Rz

#------------------- Get Distances ---------------------------------------------
def get_distances(cube, Rz, flux):
    expansionsgeschwindigkeiten = np.zeros((cube.shape[1], cube.shape[2], depth_3d))
    with open(output_path, 'w') as file:
        file.write('x_pixel,y_pixel,R_x(pc),R_y(pc),R_z(pc),v_exp(km/s,)Flux()\n')
        flux_max = 0
        for y in range(cube.shape[1]):
            for x in range(cube.shape[2]):
                for z in range(depth_3d):
                    rel_x = x - x_center
                    rel_y = y - y_center
                    Rx = (rel_x * theta / 206265) * d
                    Ry = (rel_y * theta / 206265) * d
                    R_z = (Rz[y, x, z] / 3.086e13)
                    R_ges = np.sqrt(Rx**2 + Ry**2 + R_z**2) * 3.086e13
                    R_z = random.uniform(-1, 1) * 0.0045 + R_z
                    v_exp = R_ges / t_sn_seconds
                    expansionsgeschwindigkeiten[y, x, z] = v_exp
                    flux_actual = flux[y, x, z]
                    if flux_actual > flux_max:
                        flux_max = flux_actual
                    if flux_actual > flux_threshold:
                        file.write(f'{x},{y},{Rx:.6f},{Ry:.6f},{R_z:.6f},{v_exp:.6f},{flux_actual:.6f}\n')
    return flux_max

def main():
    filtered_cube = extract_nova_shell(cube)
    masked_cube=mask_star(filtered_cube,(y_center,x_center),aperture_radius)
    _, flux, Rz = calculate_velocity_fields(masked_cube)
    flux_max = get_distances(cube, Rz, flux)
    return

main()
