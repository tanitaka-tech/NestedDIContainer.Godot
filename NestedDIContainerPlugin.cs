using Godot;
using Godot.Collections;

namespace NestedDIContainer.Godot;

[Tool]
public partial class NestedDIContainerPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // AutoLoadに追加するスクリプトのパス
        string autoloadPath = NestedDIContainerConst.AutoloadPath;
        string autoloadName = NestedDIContainerConst.AutoloadName;

        AddAutoloadSingleton(autoloadName, autoloadPath);
        
        AddCustomProjectSettings();

        AddSceneToAutoload();
    }

    public override void _ExitTree()
    {
        string autoloadName = NestedDIContainerConst.AutoloadName;
        RemoveAutoloadSingleton(autoloadName);

        RemoveSceneFromAutoload();
    }
    
    private void AddCustomProjectSettings()
    {
        // プロジェクト設定にカスタムタブと項目を追加する
        var customSettingPath = NestedDIContainerConst.ProjectSettingsPath;
        
        // 既に設定が存在する場合はスキップ
        if (!ProjectSettings.HasSetting(customSettingPath))
        {
            ProjectSettings.SetSetting(customSettingPath, "");
        }
        
        ProjectSettings.AddPropertyInfo(new Dictionary()
        {
            { "name", customSettingPath },
            { "type", (int)Variant.Type.String },
            { "usage", (int)PropertyUsageFlags.Default },
            { "hint", (int)PropertyHint.File },
            { "hint_string", "*.tscn" }
        });
        GD.Print("Add NestedDIContainer project settings");

        // プロジェクト設定の変更を保存
        ProjectSettings.Save();
    }
    
    private void AddSceneToAutoload()
    {
        var customSettingName = NestedDIContainerConst.ProjectSettingsPath;
        string autoloadName = NestedDIContainerConst.AutoloadName;


        if (ProjectSettings.HasSetting(customSettingName))
        {
            string scenePath = (string)ProjectSettings.GetSetting(customSettingName);

            // 既に登録されていない場合のみ追加
            if (!string.IsNullOrEmpty(scenePath) && !Engine.HasSingleton(autoloadName))
            {
                AddAutoloadSingleton(autoloadName, scenePath);
                GD.Print("Added " + autoloadName + " to AutoLoad");
            }
        }
    }

    private void RemoveSceneFromAutoload()
    {
        string autoloadName = NestedDIContainerConst.AutoloadName;

        if (Engine.HasSingleton(autoloadName))
        {
            RemoveAutoloadSingleton(autoloadName);
            GD.Print("Removed " + autoloadName + " from AutoLoad");
        }
    }
}