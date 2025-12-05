import matplotlib.pyplot as plt
import numpy as np
from enum import Enum, auto

class PlotType(Enum):
    CRIT = auto()
    HIT = auto()
    RUN = auto()
    STATCONTEST = auto()

plotSelector = PlotType.STATCONTEST

match(plotSelector):
    case PlotType.CRIT:
        t = np.arange(-50,50,0.1)
        s = 0.4 * (0.5 + np.arctan((t-20)/10) / np.pi)
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case PlotType.HIT:
        t = np.arange(-50,50,0.1)
        s = 0.85 + 0.15 * np.arctan((t+8)/8) / np.pi
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case PlotType.RUN:
        t = np.arange(-50,50,0.1)
        s = (0.5 + np.arctan(t/20) / np.pi)
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()
    case PlotType.STATCONTEST:
        t = np.arange(-50,50,0.1)
        s = (0.5 + np.arctan(t/10) / np.pi)
        fig, ax = plt.subplots()
        ax.plot(t,s)
        plt.show()

    case _:
        raise ValueError("Invalid plot type selected")
