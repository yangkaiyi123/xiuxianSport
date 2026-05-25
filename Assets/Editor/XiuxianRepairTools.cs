using UnityEditor;

public static class XiuxianRepairTools
{
    [MenuItem("Xiuxian/Force Reimport Scripts")]
    public static void ForceReimportScripts()
    {
        AssetDatabase.ImportAsset("Assets/Scripts/XiuxianFitnessApp.cs", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
