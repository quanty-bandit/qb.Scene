# qb.Scene
Scene loading management

**SceneLoader**

Singleton manager for loading and unloading Unity scenes, supporting both built-in and addressable scenes, with progress reporting and event channels.

**SceneList**

ScriptableObject asset that holds a list of scene entries for use in Unity projects, providing indexed and named access as well as enumeration support.
This object are used as entry by the SceneLoader behaviour, it can be create with the Unity editor context menu [Create/qb/SceneList] 

## HOW TO INSTALL

Use the Unity package manager and the Install package from git url option.

- Install at first time,if you haven't already done so previously, the package <mark>[unity-package-manager-utilities](https://github.com/sandolkakos/unity-package-manager-utilities.git)</mark> from the following url: 
  [GitHub - sandolkakos/unity-package-manager-utilities: That package contains a utility that makes it possible to resolve Git Dependencies inside custom packages installed in your Unity project via UPM - Unity Package Manager.](https://github.com/sandolkakos/unity-package-manager-utilities.git)

- Next, install the package from the current package git URL. 
  
  All other dependencies of the package should be installed automatically.

## Dependencies

- https://github.com/quanty-bandit/qb.Pattern.git
- https://github.com/quanty-bandit/qb.Events.git
- - [GitHub - codewriter-packages/Tri-Inspector: Free inspector attributes for Unity [Custom Editor, Custom Inspector, Inspector Attributes, Attribute Extensions]](https://github.com/codewriter-packages/Tri-Inspector.git)
