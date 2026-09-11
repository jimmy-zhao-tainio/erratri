"""Render the actual STL: python Demo.Loft.Shell/render_preview.py [mesh.stl]."""
import sys
from pathlib import Path
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

path = Path(sys.argv[1] if len(sys.argv) > 1 else "twisted_shell.stl")
data = path.read_bytes()
count = int.from_bytes(data[80:84], "little")
record = np.dtype([("normal", "<f4", (3,)), ("vertices", "<f4", (3, 3)), ("attribute", "<u2")])
assert len(data) == 84 + 50 * count
triangles = np.frombuffer(data, dtype=record, count=count, offset=84)["vertices"].astype(float)
normal = np.cross(triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0])
normal /= np.linalg.norm(normal, axis=1)[:, None]
light = np.array([-0.4, -0.6, 0.7]); light /= np.linalg.norm(light)
shade = 0.32 + 0.68 * np.maximum(0, normal @ light)
colors = np.clip(shade[:, None] * np.array([0.25, 0.85, 0.78])[None, :], 0, 1)
low = triangles.min(axis=(0, 1)); high = triangles.max(axis=(0, 1))
fig = plt.figure(figsize=(13, 8), facecolor="#101921")
for index, (elevation, azimuth, label) in enumerate([(24, -65, "TWIST + FLARE"), (65, 115, "OPEN BORE")]):
    ax = fig.add_subplot(1, 2, index + 1, projection="3d", facecolor="#101921")
    ax.add_collection3d(Poly3DCollection(triangles, facecolors=colors, edgecolors=(0.04, 0.12, 0.15, 0.23), linewidths=0.22))
    ax.set_xlim(low[0] - 5, high[0] + 5); ax.set_ylim(low[1] - 5, high[1] + 5); ax.set_zlim(0, high[2])
    ax.set_box_aspect(high - low + np.array([10, 10, 0]))
    ax.view_init(elev=elevation, azim=azimuth)
    ax.set_axis_off()
    ax.set_title(label, color="#8ba5b4", fontsize=10, pad=-10)
fig.text(0.07, 0.93, "ERRATRI / TWISTED SHELL", color="white", fontsize=22, weight="bold")
fig.text(0.07, 0.885, "33 planar sections  /  105° twist  /  140 mm tall  /  integer-grid geometry", color="#9ab1bd", fontsize=11)
fig.subplots_adjust(left=0, right=1, top=0.84, bottom=0.02, wspace=-0.12)
output = path.with_suffix(".png")
fig.savefig(output, dpi=160, facecolor=fig.get_facecolor())
print(output.resolve())
