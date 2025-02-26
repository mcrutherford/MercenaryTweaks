**1.3.0**
- Updated for Seekers of the Storm

**1.2.6**

- added a git repo, and linked it on the mod page

**1.2.5**

- fixed EvisSlayer only being enabled when SingletargetEvis was disabled
- fixed incompatibilities with a several other mods due to instances of "ExamplePlugin" in the code and name

**1.2.4**

- fixed EvisSlayer only being enabled when SingleTargetEvis was enabled

**1.2.3**

- fixed EvisSlayer just, not working this entire time, my bad

**1.2.2**

- removed a line of code that caused ArgumentNullException errors anytime you used eviscerate

**1.2.1**

- fixed M1AttackSpeedFix not working as intended

**1.2.0**

- finally updated for sotv! no longer requires R2API

**1.1.4**

- fixed the previous fix only applying when SingleTargetEvis was enabled

**1.1.3**

- fixed Eviscerate targetting allies even when unable to damage them, which was a vanilla bug, made worse by completely burning your evis if SingleTargetEviscerate targeted an ally

**1.1.2**

- fixed the plugin name and version being outdated in the dll, causing the config to use the old mod name

**1.1.1**

- fixed a typo on the mod page, "give you a jump"

**1.1.0**

- fixed SingleTargetEviscerate never switching targets (ty Withor)
- changed Massacre to make you re-enter the evis state, rather than just resetting the duration itself, which should help prevent future issues (ty again Withor)
- added EvisSlayer and EvisDamage, for more customizability and synergy with other tweaks (Withor helped with these too)
- fixed a couple tweaks claiming to be unimplemented in the configs, despite being implemented on mod release
- re-ordered the change log to show the most recent update first, not sure why I didn't do that to begin with

**1.0.1**

- fixed an issue with SingleTargetEviscerate not being disabled when set to false, currently working on issues with it not switching targets once the initial target dies

**1.0.0**

- released