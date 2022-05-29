import matplotlib.pyplot as plt
import numpy as np

def SAT( v):
    return np.clip(v,0,1)
    # return np.minimum(1, np.maximum(v, 0))

def remap(v,l0,h0,ln,hn):
    return ln+(v-l0)*(hn-ln)/(h0-l0)


xpoints = np.arange(0,1,0.01)

m0 = remap(xpoints,0,0.07,0,1)
ypoints = SAT(m0)

m1 = remap(xpoints,0.4,1,1,0)
zpoints = SAT(m1)

m2 = remap(xpoints, 0, 0.15, 0, 1)
vpoints = xpoints * SAT(m2)

m3 = remap(xpoints, 0.9, 1.0, 1, 0)
upoints = SAT(m3)

gd = 1
wd = 1
wpoints = gd*vpoints*upoints*wd*2



# plt.plot(xpoints, ypoints)
# plt.plot(xpoints, zpoints)
# plt.plot(xpoints, ypoints*zpoints)

plt.plot(xpoints, vpoints)
plt.plot(xpoints, upoints)
plt.plot(xpoints, wpoints)

plt.show()