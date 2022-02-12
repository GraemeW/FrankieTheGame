import matplotlib.pyplot as plt
import numpy as np

# Crit Plots
t = np.arange(-50,50,0.1)
s = 0.4 * (0.5 + np.arctan((t-20)/10) / np.pi)
fig, ax = plt.subplots()
ax.plot(t,s)
plt.show()

# Hit Plots
t = np.arange(-50,50,0.1)
s = 0.5 + np.arctan((t+8)/8) / np.pi
fig, ax = plt.subplots()
ax.plot(t,s)
plt.show()