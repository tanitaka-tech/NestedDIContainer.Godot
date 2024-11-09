using Godot;

namespace NestedDIContainer.Godot;

public partial class ProjectContextLoadNode : Node
{
    public override void _EnterTree()
    {
        var customSettingName = NestedDIContainerConst.ProjectSettingsPath;

        if (ProjectSettings.HasSetting(customSettingName))
        {
            string scenePath = (string)ProjectSettings.GetSetting(customSettingName);

            if (!string.IsNullOrEmpty(scenePath))
            {
                PackedScene packedScene = (PackedScene)ResourceLoader.Load(scenePath);

                Node instance = packedScene.Instantiate();


                AddChild(instance);
            }
            else
            {
                GD.Print(　$"ScenePath is not found: {scenePath}");
            }
        }
    }
}