import matplotlib.pyplot as plt
import numpy as np
from enum import Enum, auto

class PlotType(Enum):
    CRIT = auto()
    HIT = auto()
    RUN = auto()
    STATCONTEST = auto()
    FEARSOME = auto()

plotSelector = PlotType.FEARSOME
level = 6
enemyLevel = 5

# Defaults
t = np.arange(-50,50,0.1)
s = np.zeros_like(t)

# Crunching
match(plotSelector):
    case PlotType.CRIT:
        t = np.arange(-50,50,0.1)
        s = 0.4 * (0.5 + np.arctan((t-20)/10) / np.pi)
    case PlotType.HIT:
        t = np.arange(-50,50,0.1)
        s = 0.85 + 0.15 * np.arctan((t+8)/8) / np.pi
    case PlotType.RUN:
        t = np.arange(-50,50,0.1)
        s = (0.5 + np.arctan(t/20) / np.pi)
    case PlotType.STATCONTEST:
        t = np.arange(-50,50,0.1)
        s = (0.5 + np.arctan(t/10) / np.pi)
    case PlotType.FEARSOME:
        t = np.arange(-50,50,0.1)
        s = t / (10 * enemyLevel / level) -1
    case _:
        raise ValueError("Invalid plot type selected")

# Plotting
fig, ax = plt.subplots()
ax.plot(t,s)
plt.grid(True)
plt.show()
