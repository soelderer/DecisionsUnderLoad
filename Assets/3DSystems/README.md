# Haptics Direct for Unity V1

You need to install the Haptics Direct for Unity V1 plugin. We cannot include it in the repository due to the proprietary licence.
However, you can obtain the plugin for free in the AssetStore:
https://assetstore.unity.com/packages/tools/integration/haptics-direct-for-unity-v1-197034

In order to sample the device's position with 1kHz like in our application, you have to patch the plugin, because it is not thread-safe. I decided to wrap every API-call in a lock statement, which works well.
Don't hesitate to get in touch for implementation details!
