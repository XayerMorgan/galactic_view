"""
Blender 5.2 Python Script: Procedural Cosmic Assets Generator
Generates high-fidelity 3D assets for Unity across all 4 cosmic zoom stages:
Stage 1: Solar System
Stage 2: Milky Way Galaxy
Stage 3: Local Group
Stage 4: Observable Universe & Cosmic Web
"""

import bpy
import bmesh
import math
import os

OUTPUT_DIR = os.path.abspath(r"d:\Vibe Code Repo\galactic_view\unity_cosmic_engine\Assets\Models")
os.makedirs(OUTPUT_DIR, exist_ok=True)

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_material(name, color_rgba, emission_strength=0.0, roughness=0.5, metallic=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    node_bsdf.inputs['Base Color'].default_value = color_rgba
    node_bsdf.inputs['Roughness'].default_value = roughness
    node_bsdf.inputs['Metallic'].default_value = metallic
    
    if emission_strength > 0:
        if 'Emission Color' in node_bsdf.inputs:
            node_bsdf.inputs['Emission Color'].default_value = color_rgba
            node_bsdf.inputs['Emission Strength'].default_value = emission_strength
        elif 'Emission' in node_bsdf.inputs:
            node_bsdf.inputs['Emission'].default_value = color_rgba
            
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    return mat

def create_ring_mesh(name, inner_r, outer_r, segments=64):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    inner_verts = []
    outer_verts = []
    
    for i in range(segments):
        theta = (i / segments) * 2.0 * math.pi
        x_in = math.cos(theta) * inner_r
        y_in = math.sin(theta) * inner_r
        x_out = math.cos(theta) * outer_r
        y_out = math.sin(theta) * outer_r
        
        v_in = bm.verts.new((x_in, y_in, 0.0))
        v_out = bm.verts.new((x_out, y_out, 0.0))
        inner_verts.append(v_in)
        outer_verts.append(v_out)
        
    bm.verts.ensure_lookup_table()
    for i in range(segments):
        next_i = (i + 1) % segments
        bm.faces.new([inner_verts[i], outer_verts[i], outer_verts[next_i], inner_verts[next_i]])
        
    bm.to_mesh(mesh)
    bm.free()
    return obj

# -------------------------------------------------------------
# 1. BUILD SOLAR SYSTEM ASSETS
# -------------------------------------------------------------
print("Generating Solar System Models...")
clear_scene()

mat_sun = create_material("Mat_Sun", (1.0, 0.95, 0.8, 1.0), emission_strength=4.5)
mat_mercury = create_material("Mat_Mercury", (0.6, 0.6, 0.62, 1.0), roughness=0.8)
mat_venus = create_material("Mat_Venus", (0.9, 0.78, 0.45, 1.0), roughness=0.5)
mat_earth = create_material("Mat_Earth", (0.15, 0.55, 0.95, 1.0), roughness=0.3, metallic=0.1)
mat_moon = create_material("Mat_Moon", (0.75, 0.75, 0.78, 1.0), roughness=0.9)
mat_mars = create_material("Mat_Mars", (0.85, 0.35, 0.22, 1.0), roughness=0.7)
mat_jupiter = create_material("Mat_Jupiter", (0.85, 0.65, 0.4, 1.0), roughness=0.5)
mat_saturn = create_material("Mat_Saturn", (0.9, 0.82, 0.55, 1.0), roughness=0.5)
mat_saturn_rings = create_material("Mat_Saturn_Rings", (0.85, 0.78, 0.5, 0.8), emission_strength=0.2)
mat_uranus = create_material("Mat_Uranus", (0.4, 0.85, 0.9, 1.0), roughness=0.4)
mat_neptune = create_material("Mat_Neptune", (0.18, 0.42, 0.95, 1.0), roughness=0.4)
mat_boundary = create_material("Mat_Neptune_Boundary", (0.22, 0.75, 1.0, 1.0), emission_strength=2.0)

# Sun
bpy.ops.mesh.primitive_uv_sphere_add(radius=3.5, segments=32, ring_count=24, location=(0, 0, 0))
sun_obj = bpy.context.active_object
sun_obj.name = "Sun_Core"
sun_obj.data.materials.append(mat_sun)

# Planetary bodies data: (name, radius, distance, material)
planets = [
    ("Mercury", 0.4, 8.0, mat_mercury),
    ("Venus", 0.7, 13.0, mat_venus),
    ("Earth", 0.8, 19.0, mat_earth),
    ("Mars", 0.5, 26.0, mat_mars),
    ("Jupiter", 2.2, 40.0, mat_jupiter),
    ("Saturn", 1.8, 56.0, mat_saturn),
    ("Uranus", 1.1, 72.0, mat_uranus),
    ("Neptune", 1.1, 90.0, mat_neptune)
]

for name, r, dist, mat in planets:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=24, ring_count=16, location=(dist, 0, 0))
    p_obj = bpy.context.active_object
    p_obj.name = name
    p_obj.data.materials.append(mat)
    
    if name == "Earth":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.22, segments=16, ring_count=12, location=(dist + 1.8, 0, 0))
        m_obj = bpy.context.active_object
        m_obj.name = "Moon"
        m_obj.data.materials.append(mat_moon)
        m_obj.parent = p_obj
        
    elif name == "Saturn":
        ring_obj = create_ring_mesh("Saturn_Rings", r * 1.35, r * 2.5, segments=64)
        ring_obj.location = (dist, 0, 0)
        ring_obj.rotation_euler = (math.radians(26.7), 0, 0)
        ring_obj.data.materials.append(mat_saturn_rings)
        ring_obj.parent = p_obj

# Neptune boundary orbit line ribbon
boundary_ribbon = create_ring_mesh("Neptune_Orbit_Boundary", 89.6, 90.4, segments=128)
boundary_ribbon.data.materials.append(mat_boundary)

# Export Solar System FBX
fbx_sol = os.path.join(OUTPUT_DIR, "solar_system_bodies.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_sol, use_selection=False)
print("Saved:", fbx_sol)


# -------------------------------------------------------------
# 2. BUILD MILKY WAY GALAXY ASSETS
# -------------------------------------------------------------
print("Generating Milky Way Models...")
clear_scene()

mat_core = create_material("Mat_MW_Core", (1.0, 0.9, 0.7, 1.0), emission_strength=3.0)
mat_disk = create_material("Mat_MW_Spiral_Disk", (0.35, 0.65, 1.0, 0.8), emission_strength=1.5)
mat_sgra = create_material("Mat_SgrA_Hole", (0.02, 0.02, 0.02, 1.0), roughness=0.1)
mat_sun_pos = create_material("Mat_Sun_Marker", (1.0, 0.85, 0.2, 1.0), emission_strength=4.0)

# Central supermassive black hole Sgr A*
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.8, segments=32, ring_count=24, location=(0, 0, 0))
sgra_obj = bpy.context.active_object
sgra_obj.name = "Sagittarius_A_Horizon"
sgra_obj.data.materials.append(mat_sgra)

# Sgr A* Accretion ring
accretion = create_ring_mesh("SgrA_Accretion_Disk", 2.2, 4.5, segments=64)
accretion.rotation_euler = (math.radians(35), math.radians(20), 0)
accretion.data.materials.append(mat_core)

# Central Bulge Ellipsoid
bpy.ops.mesh.primitive_uv_sphere_add(radius=8.0, segments=32, ring_count=24, location=(0, 0, 0))
bulge_obj = bpy.context.active_object
bulge_obj.name = "Galactic_Bulge"
bulge_obj.scale = (1.5, 1.5, 0.6)
bpy.ops.object.transform_apply(scale=True)
bulge_obj.data.materials.append(mat_core)

# Procedural Multi-Arm Spiral Geometry
mesh_mw = bpy.data.meshes.new("MilkyWay_Spiral_Arms")
obj_mw = bpy.data.objects.new("MilkyWay_Spiral_Arms", mesh_mw)
bpy.context.collection.objects.link(obj_mw)

bm_mw = bmesh.new()
num_arms = 4
steps_per_arm = 80
arm_length = 65.0

for a in range(num_arms):
    arm_angle_offset = (a / num_arms) * 2.0 * math.pi
    prev_v_in = None
    prev_v_out = None
    
    for s in range(steps_per_arm):
        t = s / steps_per_arm
        rad = 6.0 + t * arm_length
        theta = arm_angle_offset + t * 3.8 * math.pi
        
        width = (3.5 + t * 6.0)
        thick = max(0.5, (1.0 - t) * 2.5)
        
        # Center of arm slice
        cx = math.cos(theta) * rad
        cy = math.sin(theta) * rad
        
        # Normal direction for arm width
        nx = -math.sin(theta)
        ny = math.cos(theta)
        
        v_in = bm_mw.verts.new((cx - nx * width * 0.5, cy - ny * width * 0.5, thick * 0.5))
        v_out = bm_mw.verts.new((cx + nx * width * 0.5, cy + ny * width * 0.5, -thick * 0.5))
        
        if prev_v_in and prev_v_out:
            bm_mw.faces.new([prev_v_in, prev_v_out, v_out, v_in])
            
        prev_v_in = v_in
        prev_v_out = v_out

bm_mw.to_mesh(mesh_mw)
bm_mw.free()
obj_mw.data.materials.append(mat_disk)

# Sun's position beacon on Orion Arm (scaled ~26,000 LY from Core)
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.2, segments=16, ring_count=12, location=(28.0, 6.0, 1.0))
sun_marker = bpy.context.active_object
sun_marker.name = "Sun_Position_Orion_Arm"
sun_marker.data.materials.append(mat_sun_pos)

# Export Milky Way FBX
fbx_mw = os.path.join(OUTPUT_DIR, "milky_way_spiral.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_mw, use_selection=False)
print("Saved:", fbx_mw)


# -------------------------------------------------------------
# 3. BUILD LOCAL GROUP ASSETS
# -------------------------------------------------------------
print("Generating Local Group Models...")
clear_scene()

mat_mw_lg = create_material("Mat_LG_MilkyWay", (0.35, 0.75, 1.0, 1.0), emission_strength=2.5)
mat_m31 = create_material("Mat_LG_Andromeda", (0.95, 0.85, 0.6, 1.0), emission_strength=2.8)
mat_m33 = create_material("Mat_LG_Triangulum", (0.4, 0.95, 0.65, 1.0), emission_strength=2.0)
mat_dwarf = create_material("Mat_LG_Dwarf", (0.85, 0.55, 0.95, 1.0), emission_strength=1.8)
mat_grav = create_material("Mat_LG_Gravity_Tether", (0.95, 0.65, 0.15, 0.6), emission_strength=1.5)

# Milky Way Disk in Local Group
bpy.ops.mesh.primitive_cylinder_add(radius=12.0, depth=1.2, vertices=32, location=(-20.0, 0, 0))
mw_lg = bpy.context.active_object
mw_lg.name = "LG_MilkyWay"
mw_lg.rotation_euler = (math.radians(15), 0, 0)
mw_lg.data.materials.append(mat_mw_lg)

# Andromeda Galaxy (M31, 2.54 MLY, massive disk tilted at 77 deg)
bpy.ops.mesh.primitive_cylinder_add(radius=20.0, depth=1.6, vertices=48, location=(55.0, 18.0, -22.0))
m31_lg = bpy.context.active_object
m31_lg.name = "LG_Andromeda_M31"
m31_lg.rotation_euler = (math.radians(77.0), math.radians(35.0), 0)
m31_lg.data.materials.append(mat_m31)

# Triangulum Galaxy (M33, 2.73 MLY)
bpy.ops.mesh.primitive_cylinder_add(radius=7.0, depth=0.9, vertices=32, location=(68.0, -22.0, 32.0))
m33_lg = bpy.context.active_object
m33_lg.name = "LG_Triangulum_M33"
m33_lg.rotation_euler = (math.radians(-42.0), math.radians(20.0), 0)
m33_lg.data.materials.append(mat_m33)

# Dwarf galaxies (LMC, SMC, M32, M110)
dwarfs = [
    ("Large_Magellanic_Cloud", 2.2, (-14.0, -8.0, 6.0)),
    ("Small_Magellanic_Cloud", 1.4, (-17.0, -11.0, 11.0)),
    ("M32_Andromeda_Satellite", 1.2, (52.0, 16.0, -20.0)),
    ("M110_Andromeda_Satellite", 1.6, (58.0, 21.0, -25.0)),
    ("Leo_I_Dwarf", 1.0, (-5.0, 24.0, -8.0)),
    ("Sculptor_Dwarf", 1.0, (-18.0, -18.0, -14.0))
]
for dname, drad, dloc in dwarfs:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=drad, segments=16, ring_count=12, location=dloc)
    dobj = bpy.context.active_object
    dobj.name = dname
    dobj.data.materials.append(mat_dwarf)

# Gravitational Vector Line (Curved bridge connecting MW and M31)
mesh_grav = bpy.data.meshes.new("MW_M31_Gravitational_Tether")
obj_grav = bpy.data.objects.new("MW_M31_Gravitational_Tether", mesh_grav)
bpy.context.collection.objects.link(obj_grav)

bm_grav = bmesh.new()
p0 = mw_lg.location
p2 = m31_lg.location
p1_coord = ((p0.x + p2.x)*0.5, (p0.y + p2.y)*0.5 + 15.0, (p0.z + p2.z)*0.5 - 8.0)

t_steps = 30
prev_v = None
for i in range(t_steps + 1):
    t = i / t_steps
    # Quadratic bezier interpolation
    x = (1-t)**2 * p0.x + 2*(1-t)*t * p1_coord[0] + t**2 * p2.x
    y = (1-t)**2 * p0.y + 2*(1-t)*t * p1_coord[1] + t**2 * p2.y
    z = (1-t)**2 * p0.z + 2*(1-t)*t * p1_coord[2] + t**2 * p2.z
    v = bm_grav.verts.new((x, y, z))
    if prev_v:
        bm_grav.edges.new([prev_v, v])
    prev_v = v

bm_grav.to_mesh(mesh_grav)
bm_grav.free()
obj_grav.data.materials.append(mat_grav)

# Export Local Group FBX
fbx_lg = os.path.join(OUTPUT_DIR, "local_group_galaxies.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_lg, use_selection=False)
print("Saved:", fbx_lg)


# -------------------------------------------------------------
# 4. BUILD OBSERVABLE UNIVERSE & COSMIC WEB ASSETS
# -------------------------------------------------------------
print("Generating Observable Universe & Cosmic Web Models...")
clear_scene()

mat_horizon = create_material("Mat_Observable_Horizon_Boundary", (0.25, 0.7, 1.0, 0.4), emission_strength=1.8)
mat_web = create_material("Mat_Cosmic_Web_Filaments", (0.95, 0.8, 0.4, 0.8), emission_strength=2.2)
mat_laniakea = create_material("Mat_Laniakea_Center", (1.0, 0.4, 0.2, 1.0), emission_strength=4.0)

# Observable Universe Particle Horizon Shell (IcoSphere Geodesic)
bpy.ops.mesh.primitive_ico_sphere_add(radius=120.0, subdivisions=4, location=(0, 0, 0))
horizon_obj = bpy.context.active_object
horizon_obj.name = "Observable_Universe_Particle_Horizon"
horizon_obj.data.materials.append(mat_horizon)

# Horizon Equator Ring
horiz_equator = create_ring_mesh("Horizon_Equator_Ring", 119.2, 120.5, segments=128)
horiz_equator.data.materials.append(mat_horizon)

# Laniakea / Virgo Supercluster Basin Center
bpy.ops.mesh.primitive_uv_sphere_add(radius=4.5, segments=24, ring_count=16, location=(0, 0, 0))
lan_obj = bpy.context.active_object
lan_obj.name = "Laniakea_Supercluster_Node"
lan_obj.data.materials.append(mat_laniakea)

# Procedural Cosmic Web Filament Spongiform Mesh
mesh_web = bpy.data.meshes.new("Cosmic_Web_Filaments")
obj_web = bpy.data.objects.new("Cosmic_Web_Filaments", mesh_web)
bpy.context.collection.objects.link(obj_web)

bm_web = bmesh.new()
# Generate interconnected filament trunks radiating outward to nodes
supercluster_nodes = [
    (0.0, 0.0, 0.0), # Laniakea
    (38.0, 25.0, -18.0), # Perseus-Pisces
    (-42.0, 15.0, 32.0), # Coma Supercluster
    (22.0, -45.0, 15.0), # Shapley Attractor
    (-28.0, -35.0, -38.0), # Hercules Supercluster
    (65.0, -12.0, -48.0), # Horologium-Reticulum
    (-60.0, 48.0, -25.0)  # Corona Borealis
]

node_verts = [bm_web.verts.new(pos) for pos in supercluster_nodes]
bm_web.verts.ensure_lookup_table()

for i in range(len(node_verts)):
    for j in range(i + 1, len(node_verts)):
        v1 = node_verts[i]
        v2 = node_verts[j]
        dist = (v1.co - v2.co).length
        if dist < 85.0:
            # Subdivide edge into filament strand
            strand_steps = 8
            prev_sv = v1
            for s in range(1, strand_steps):
                t = s / strand_steps
                interp = v1.co.lerp(v2.co, t)
                # Perturb filament with sinusoidal cosmic waviness
                noise_x = math.sin(t * math.pi * 3 + i) * 3.5
                noise_y = math.cos(t * math.pi * 3 + j) * 3.5
                noise_z = math.sin(t * math.pi * 2 + i + j) * 3.5
                sv = bm_web.verts.new((interp.x + noise_x, interp.y + noise_y, interp.z + noise_z))
                bm_web.edges.new([prev_sv, sv])
                prev_sv = sv
            bm_web.edges.new([prev_sv, v2])

bm_web.to_mesh(mesh_web)
bm_web.free()
obj_web.data.materials.append(mat_web)

# Export Observable Universe FBX
fbx_uni = os.path.join(OUTPUT_DIR, "observable_universe_boundary.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_uni, use_selection=False)
print("Saved:", fbx_uni)

print("\n--- ALL 4 STAGE 3D MODELS GENERATED SUCCESSFULLY ---")
