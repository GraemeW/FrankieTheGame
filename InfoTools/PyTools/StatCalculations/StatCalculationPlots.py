import matplotlib.pyplot as plt
import numpy as np
from enum import Enum, auto

class PlotType(Enum):
    CRIT = auto()
    HIT = auto()
    RUN = auto()
    STATCONTEST = auto()
    FEARSOME = auto()
    MOVESPEED = auto()

plotSelector = PlotType.MOVESPEED
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
    case PlotType.MOVESPEED:
        t = np.arange(-100,200,0.1)
        s = np.zeros_like(t)
        minMove = 0.5
        maxMove = 2.0
        negativeSlope = 0.025
        positiveSlope = 0.0075

        for i,x in enumerate(t):
            if x < 0:
                s[i] = 1.0 + (1.0 - minMove) * (negativeSlope * x) / np.sqrt(1 + (negativeSlope * x)**2)
            elif x == 0:
                s[i] = 1.0
            else:
                s[i] = 1.0 + (maxMove - 1.0) * (positiveSlope * x) / np.sqrt(1 + (positiveSlope * x)**2)

    case _:
        raise ValueError("Invalid plot type selected")

# Plotting
fig, ax = plt.subplots()
ax.plot(t,s)
plt.grid(True)
plt.show()
