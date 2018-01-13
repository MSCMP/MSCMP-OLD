# Debugging guide

To debug the managed code of the mod `MSCMPClient` you have to.

1. Download and install Visual Studio Tools for Unity - https://msdn.microsoft.com/en-us/library/dn940025.aspx
2. Set environment variable `UNITY_GIVE_CHANCE_TO_ATTACH_DEBUGGER` to `1`.
3. After launching MSCMP normal way the dialog asking you to attach debugger will appear. Now go to Visual Studio, in **Debug** tab select **Attach Unity Debugger**.
4. Done! You can now put breakpoints and debug the code.
