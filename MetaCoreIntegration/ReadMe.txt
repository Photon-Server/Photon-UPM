# Meta Core Integration

## Documentation

https://doc.photonengine.com/fusion/current/industries-samples/industries-addons/fusion-industries-addons-metacoreintegration

## Version & Changelog

- version 2.3.4: Add MRUKPermissionWaiter and PassthroughCameraAccessPermissionWaiter to support PermissionsRequester system (useful when several components try to access Android permission request system)
- version 2.3.3: Add workaround for hand tracking jumps in MetaBridgeHardwareHand
- version 2.3.2: Remove Meta Camera sample assets (not relevant with Meta SDK v81 and further)
- version 2.3.1: Make some internal properties/methods public, to fix a build issue for Android
- version 2.3.0: Change WebCamTextureManager meta file & prefab to avoid GUID collision with MetaSDK v81 (PassthroughCameraAccess class)
- version 2.2.0: MetaHardware<...> classes renaming MetaBridgeHardware<...>
- version 2.1.2: 
  - Add Meta Camera sample assets
  - Add support for OpenXR hand in meta configurations
- version 2.1.1: Add way to position automatically transforms to match wrist and index positions
- version 2.1.0: Update to support new XRShared architecture
- Version 2.0.3: Remove VisionOSHelpers dependency 
- Version 2.0.2: Add verification for WebGL build
- Version 2.0.1: 
  - Add Meta interaction SDK check and define addition
  - Enhance hand prefab's index position
- Version 2.0.0: First release
