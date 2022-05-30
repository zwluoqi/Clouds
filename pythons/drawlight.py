import matplotlib.pyplot as plt
import numpy as np



xpoints = np.arange(0,80,1)
ypoints = np.power(xpoints/700.0,4)
zpoints = np.power(xpoints/530.0,4)
wpoints = np.power(xpoints/400.0,4)



# plt.plot(xpoints, ypoints)
# plt.plot(xpoints, zpoints)
# plt.plot(xpoints, ypoints*zpoints)

plt.plot(xpoints, ypoints)
plt.plot(xpoints, zpoints)
plt.plot(xpoints, wpoints)

plt.show()