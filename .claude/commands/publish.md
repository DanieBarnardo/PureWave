Publish PureWave to production via FTP.

Run the following command from `D:/Development/PureWave`:

```
dotnet publish PureWave.Web/PureWave.Web.csproj -p:PublishProfile=FTPProfile -p:Password="V:nltDD*766sP8"
```

If the publish step succeeds but FTP upload fails with a 550 error (DLL locked by IIS), use the app_offline.htm workaround:
1. Upload `app_offline.htm` to the FTP root to take the app offline and release the file lock
2. Wait 3 seconds
3. Re-run the publish command
4. Delete `app_offline.htm` from FTP to bring the app back online

Report the outcome to the user when done.
