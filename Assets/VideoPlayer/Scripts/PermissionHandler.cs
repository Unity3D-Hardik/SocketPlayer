using UnityEngine.Android;
using UnityEngine;

public class PermissionHandler : MonoBehaviour
{

    private void Awake()
    {
        RequestStoragePermissions();
    }

    private void OnApplicationFocus(bool focus)
    {
        RequestStoragePermissions();
    }

    public void RequestStoragePermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) ||
            !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermissions(new[] { Permission.ExternalStorageRead, Permission.ExternalStorageWrite});
        }
    }
}