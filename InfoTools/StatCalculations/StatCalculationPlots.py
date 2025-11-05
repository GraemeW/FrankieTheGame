import matplotlib.pyplot as plt
import numpy as np
from enum import Enum, auto

class PlotType(Enum):
    CRIT = auto()
    HIT = auto()
    RUN = auto()

plotSelector = PlotType.RUN

match(plotSelector):
    case PlotType.CRIT:
        # Crit Plots
        t = np.arange(-50,50,0.1)
        s = 0.4 * (0.5 + np.arctan((t-20)/10) / np.pi)
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case PlotType.HIT:
        # Hit Plots
        t = np.arange(-50,50,0.1)
        s = 0.85 + 0.15 * np.arctan((t+8)/8) / np.pi
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case PlotType.RUN:
        # Run Plots
        t = np.arange(-50,50,0.1)
        s = (0.5 + np.arctan(t/20) / np.pi)
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case _:
        raise ValueError("Invalid plot type selected")
