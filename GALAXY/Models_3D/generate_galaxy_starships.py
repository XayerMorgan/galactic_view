"""
Blender 5.2 Automation Pipeline for Project GALAXY
Procedurally models and exports high-detail 3D Sci-Fi Starships:
1. USS Astronautica Flagship (Saucer hull, dual warp pylons, glowing cyan nacelles, deflector array)
2. Sol Scout Fighter (Sleek angular lifting body, dual ion exhausts, cockpit canopy)
Exports both .glb and .fbx formats with PBR materials for Unity 6 Standalone and native Blender rendering.
"""

import bpy
import math
import os
import sys

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_emissive_material(name, color, emission_strength=5.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_emit = nodes.new(type='ShaderNodeEmission')
    node_emit.inputs['Color'].default_value = color
    node_emit.inputs['Strength'].default_value = emission_strength
    
    mat.node_tree.links.new(node_emit.outputs['Emission'], node_out.inputs['Surface'])
    return mat

def create_hull_material(name, base_color=(0.15, 0.18, 0.22, 1.0), roughness=0.3, metallic=0.85):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = base_color
        bsdf.inputs['Roughness'].default_value = roughness
        bsdf.inputs['Metallic'].default_value = metallic
    return mat

def build_uss_astronautica():
    clear_scene()
    
    mat_hull = create_hull_material("Mat_FlagshipHull", (0.35, 0.38, 0.44, 1.0), roughness=0.35, metallic=0.7)
    mat_dark_armor = create_hull_material("Mat_DarkArmor", (0.08, 0.10, 0.14, 1.0), roughness=0.4, metallic=0.9)
    mat_warp_glow = create_emissive_material("Mat_WarpCyan", (0.0, 0.85, 1.0, 1.0), emission_strength=6.0)
    mat_deflector_glow = create_emissive_material("Mat_DeflectorBlue", (0.1, 0.5, 1.0, 1.0), emission_strength=8.0)
    mat_impulse_glow = create_emissive_material("Mat_ImpulseAmber", (1.0, 0.45, 0.05, 1.0), emission_strength=5.0)

    # 1. Primary Saucer Hull (Flattened oblate spheroid)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=10.0, location=(0, 0, 0))
    saucer = bpy.context.active_object
    saucer.name = "Flagship_Saucer_Hull"
    saucer.scale = (1.0, 1.25, 0.18)
    saucer.data.materials.append(mat_hull)

    # 2. Bridge Island Superstructure
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=2.5, location=(0, 2.0, 1.6))
    bridge = bpy.context.active_object
    bridge.name = "Flagship_Bridge_Island"
    bridge.scale = (0.7, 0.9, 0.35)
    bridge.data.materials.append(mat_dark_armor)

    # 3. Secondary Engineering Hull
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=2.6, depth=14.0, location=(0, -8.0, -3.2))
    eng_hull = bpy.context.active_object
    eng_hull.name = "Flagship_Engineering_Hull"
    eng_hull.rotation_euler = (math.radians(90), 0, 0)
    eng_hull.scale = (1.0, 0.8, 1.0)
    eng_hull.data.materials.append(mat_hull)

    # 4. Connecting Dorsal Neck
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -2.5, -1.2))
    neck = bpy.context.active_object
    neck.name = "Flagship_Connecting_Dorsal"
    neck.scale = (1.2, 5.0, 2.6)
    neck.data.materials.append(mat_dark_armor)

    # 5. Navigational Deflector Dish (Forward engineering hull)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=2.0, depth=0.8, location=(0, -1.0, -3.2))
    deflector = bpy.context.active_object
    deflector.name = "Flagship_Deflector_Dish"
    deflector.rotation_euler = (math.radians(90), 0, 0)
    deflector.data.materials.append(mat_deflector_glow)

    # 6. Warp Nacelle Pylons (Angled struts)
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 4.2, -10.0, -1.0))
        pylon = bpy.context.active_object
        pylon.name = f"Flagship_Pylon_{side}"
        pylon.scale = (5.5, 1.4, 0.35)
        pylon.rotation_euler = (0, math.radians(x_sign * 35), math.radians(x_sign * 15))
        pylon.data.materials.append(mat_dark_armor)

        # Warp Nacelle (Long aerodynamic cylinder with glowing field grilles)
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=1.1, depth=18.0, location=(x_sign * 8.5, -9.0, 1.5))
        nacelle = bpy.context.active_object
        nacelle.name = f"Flagship_WarpNacelle_{side}"
        nacelle.rotation_euler = (math.radians(90), 0, 0)
        nacelle.scale = (0.9, 1.1, 1.0)
        nacelle.data.materials.append(mat_hull)

        # Glowing Warp Nacelle Field Grille (Inner face)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * (8.5 - x_sign * 0.95), -9.0, 1.5))
        grille = bpy.context.active_object
        grille.name = f"Flagship_WarpGrille_{side}"
        grille.scale = (0.2, 14.0, 1.2)
        grille.data.materials.append(mat_warp_glow)

    # 7. Impulse Engines (Aft Saucer)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -9.5, 0.4))
    impulse = bpy.context.active_object
    impulse.name = "Flagship_Impulse_Engine"
    impulse.scale = (3.2, 0.8, 0.6)
    impulse.data.materials.append(mat_impulse_glow)

    # Join or group under master root
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.shade_smooth()

def build_sol_scout_ship():
    clear_scene()

    mat_stealth_hull = create_hull_material("Mat_StealthHull", (0.07, 0.08, 0.10, 1.0), roughness=0.3, metallic=0.95)
    mat_canopy = create_hull_material("Mat_GoldCanopy", (0.95, 0.75, 0.20, 1.0), roughness=0.1, metallic=0.9)
    mat_ion_exhaust = create_emissive_material("Mat_IonAmber", (1.0, 0.6, 0.1, 1.0), emission_strength=7.0)

    # 1. Lifting-Body Fuselage (Dart delta shape)
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=4.0, radius2=0.4, depth=12.0, location=(0, 0, 0))
    hull = bpy.context.active_object
    hull.name = "Scout_Fuselage"
    hull.rotation_euler = (math.radians(90), 0, 0)
    hull.scale = (1.2, 0.4, 1.0)
    hull.data.materials.append(mat_stealth_hull)

    # 2. Angular Wings with Cant
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 3.8, -2.0, 0.1))
        wing = bpy.context.active_object
        wing.name = f"Scout_Wing_{side}"
        wing.scale = (4.0, 5.0, 0.15)
        wing.rotation_euler = (0, math.radians(x_sign * -12), math.radians(x_sign * -25))
        wing.data.materials.append(mat_stealth_hull)

        # Winglet Fin
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 5.6, -3.0, 1.0))
        winglet = bpy.context.active_object
        winglet.name = f"Scout_Winglet_{side}"
        winglet.scale = (0.2, 3.0, 1.8)
        winglet.rotation_euler = (math.radians(-10), math.radians(x_sign * 30), 0)
        winglet.data.materials.append(mat_stealth_hull)

    # 3. Cockpit Bubble
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1.3, location=(0, 1.8, 0.6))
    canopy = bpy.context.active_object
    canopy.name = "Scout_Canopy"
    canopy.scale = (0.8, 2.0, 0.5)
    canopy.data.materials.append(mat_canopy)

    # 4. Twin Ion Thruster Nozzles
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.9, depth=2.4, location=(x_sign * 1.5, -6.0, 0))
        nozzle = bpy.context.active_object
        nozzle.name = f"Scout_Nozzle_{side}"
        nozzle.rotation_euler = (math.radians(90), 0, 0)
        nozzle.data.materials.append(mat_stealth_hull)

        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.7, depth=0.4, location=(x_sign * 1.5, -7.1, 0))
        exhaust = bpy.context.active_object
        exhaust.name = f"Scout_Exhaust_{side}"
        exhaust.rotation_euler = (math.radians(90), 0, 0)
        exhaust.data.materials.append(mat_ion_exhaust)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.shade_smooth()

def main():
    out_dir = os.path.dirname(os.path.abspath(__file__))
    os.makedirs(out_dir, exist_ok=True)

    # 1. Build & Export USS Astronautica Flagship
    print("\n[Project GALAXY] Generating 3D USS Astronautica Flagship...")
    build_uss_astronautica()
    flagship_glb = os.path.join(out_dir, "USS_Astronautica_Flagship.glb")
    flagship_fbx = os.path.join(out_dir, "USS_Astronautica_Flagship.fbx")
    flagship_blend = os.path.join(out_dir, "USS_Astronautica_Flagship.blend")
    
    bpy.ops.export_scene.gltf(filepath=flagship_glb, export_format='GLB')
    bpy.ops.export_scene.fbx(filepath=flagship_fbx)
    bpy.ops.wm.save_as_mainfile(filepath=flagship_blend)
    print(f"[Project GALAXY] Exported: {flagship_glb}")
    print(f"[Project GALAXY] Exported: {flagship_fbx}")

    # 2. Build & Export Sol Scout Ship
    print("\n[Project GALAXY] Generating 3D Sol Scout Ship...")
    build_sol_scout_ship()
    scout_glb = os.path.join(out_dir, "Sol_Scout_Ship.glb")
    scout_fbx = os.path.join(out_dir, "Sol_Scout_Ship.fbx")
    scout_blend = os.path.join(out_dir, "Sol_Scout_Ship.blend")

    bpy.ops.export_scene.gltf(filepath=scout_glb, export_format='GLB')
    bpy.ops.export_scene.fbx(filepath=scout_fbx)
    bpy.ops.wm.save_as_mainfile(filepath=scout_blend)
    print(f"[Project GALAXY] Exported: {scout_glb}")
    print(f"[Project GALAXY] Exported: {scout_fbx}")

    print("\n[Project GALAXY] Starship procedural modeling and 3D export complete!")

if __name__ == "__main__":
    main()
