#Three lines to make our compiler able to draw:
import sys
import matplotlib
# matplotlib.use('Agg')

import matplotlib.pyplot as plt
import numpy as np
# from perlin_noise import PerlinNoise


from matplotlib.widgets import Slider, Button





SOME_BIG_PRIME_NUMBER = 0x854168423648951
SOME_OTHER_NUMBER = 0x41586354



def sin_noise_fun(xpoints):
    ypoints = np.sin(xpoints)
    return ypoints
    
def simple_noise(xpoints):
    ypoints = np.array(xpoints)
    ypoints = ypoints * SOME_BIG_PRIME_NUMBER
    ypoints = ypoints + SOME_OTHER_NUMBER
    ypoints = np.multiply(ypoints,ypoints)
    zpoints = np.right_shift(ypoints,13)
    ypoints = np.power(ypoints,zpoints)
    return ypoints


def noise_fun(xpoints,amplitude,frequency,layer):
    return noise_fun1(xpoints,amplitude,frequency,layer)

def noise_fun1(xpoints,amplitude,frequency,layer):
    ypoints = np.array(xpoints)
    return ypoints


xpoints = np.arange(-100, 100,0.1,dtype = np.float32)

# t = np.linspace(0, 1, 1000)

# Define initial parameters
init_amplitude = 1
init_frequency = 1
init_layer = 1

# Create the figure and the line that we will manipulate
fig, ax = plt.subplots()
line, = plt.plot(xpoints, noise_fun(xpoints, init_amplitude, init_frequency,init_layer))
ax.set_xlabel('Time [s]')

# adjust the main plot to make room for the sliders
plt.subplots_adjust(left=0.25, bottom=0.25)

# Make a horizontal slider to control the frequency.
axfreq = plt.axes([0.25, 0.1, 0.65, 0.03])
freq_slider = Slider(
    ax=axfreq,
    label='Frequency [Hz]',
    valmin=0.1,
    valmax=30,
    valinit=init_frequency,
)


# Make a horizontal slider to control the frequency.
axLayer = plt.axes([0.25, 0.15, 0.65, 0.03])
layer_slider = Slider(
    ax=axLayer,
    label='Layer [Count]',
    valmin=1,
    valmax=30,
    valinit=init_layer,
)

# Make a vertically oriented slider to control the amplitude
axamp = plt.axes([0.25, 0.2, 0.65, 0.03])
amp_slider = Slider(
    ax=axamp,
    label="Amplitude",
    valmin=0,
    valmax=100,
    valinit=init_amplitude
    # orientation="vertical"
)


# The function to be called anytime a slider's value changes
def update(val):
    line.set_ydata(noise_fun(xpoints, amp_slider.val, freq_slider.val,layer_slider.val))
    fig.canvas.draw_idle()
# register the update function with each slider
freq_slider.on_changed(update)
amp_slider.on_changed(update)
layer_slider.on_changed(update)

# plt.plot(xpoints, ypoints)
plt.show()

#Two  lines to make our compiler able to draw:
# plt.savefig(sys.stdout.buffer)
# sys.stdout.flush()


