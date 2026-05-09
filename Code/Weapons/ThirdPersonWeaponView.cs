using Sandbox;

public sealed class ThirdPersonWeaponView : Component
{
    [Property, Group("References")]
    public ModelRenderer WeaponRenderer { get; set; }

    [Property, Group("References")]
    public PlayerRole PlayerRole { get; set; }

    [Property, Group("References")]
    public SkinnedModelRenderer BodyRenderer { get; set; }

    [Property, Group("Bone Attach")]
    public string HandBoneName { get; set; } = "hand_R";

    [Property, Group("Bone Attach")]
    public Vector3 PositionOffset { get; set; } = new Vector3(0f, 0f, 0f);

    [Property, Group("Bone Attach")]
    public Angles RotationOffset { get; set; } = new Angles(0f, 0f, 0f);

    [Property, Group("Visibility")]
    public bool HideForLocalPlayer { get; set; } = true;

    [Property, Group("Debug")]
    public bool DebugLogs { get; set; } = false;

    private GameObject _handBoneObject;

    protected override void OnStart()
    {
        WeaponRenderer ??= Components.GetInDescendantsOrSelf<ModelRenderer>();
        PlayerRole ??= Components.GetInAncestorsOrSelf<PlayerRole>();
        BodyRenderer ??= Components.GetInAncestorsOrSelf<SkinnedModelRenderer>();

        FindHandBone();
    }

    protected override void OnUpdate()
    {
        if (WeaponRenderer is null || PlayerRole is null)
            return;

        var isHuman = PlayerRole.Team == PlayerTeam.Human;

        var shouldShow = isHuman;

        if (HideForLocalPlayer && !IsProxy)
            shouldShow = false;

        WeaponRenderer.Enabled = shouldShow;

        if (!shouldShow)
            return;

        if (_handBoneObject is null || !_handBoneObject.IsValid())
        {
            FindHandBone();
        }

        if (_handBoneObject is null || !_handBoneObject.IsValid())
        {
            // Fallback, so it does not stay near the head forever.
            GameObject.LocalPosition = new Vector3(18f, -8f, 45f);
            GameObject.LocalRotation = new Angles(0f, 90f, 0f).ToRotation();
            return;
        }

        GameObject.WorldPosition =
            _handBoneObject.WorldPosition
            + _handBoneObject.WorldRotation * PositionOffset;

        GameObject.WorldRotation =
            _handBoneObject.WorldRotation * RotationOffset.ToRotation();
    }

    private void FindHandBone()
    {
        if (BodyRenderer is null || !BodyRenderer.IsValid())
            return;

        // First try SkinnedModelRenderer bone lookup.
        _handBoneObject = BodyRenderer.GetBoneObject(HandBoneName);

        if (_handBoneObject is not null && _handBoneObject.IsValid())
        {
            if (DebugLogs)
                Log.Info($"World weapon attached to bone: {HandBoneName}");

            return;
        }

        // Then try finding a child GameObject by name under the body hierarchy.
        _handBoneObject = FindChildByName(BodyRenderer.GameObject, HandBoneName);

        if (_handBoneObject is not null && _handBoneObject.IsValid())
        {
            if (DebugLogs)
                Log.Info($"World weapon attached to child object: {HandBoneName}");

            return;
        }

        string[] fallbackNames =
        {
        "hand_R_IK_target",
        "hand_L_IK_target",
        "hand_R",
        "hand_r",
        "wrist_R",
        "wrist_r",
        "weapon_R",
        "weapon_r",
        "hold_R",
        "hold_r",
        "right_hand",
        "RightHand"
    };

        foreach (var boneName in fallbackNames)
        {
            _handBoneObject = BodyRenderer.GetBoneObject(boneName);

            if (_handBoneObject is null || !_handBoneObject.IsValid())
                _handBoneObject = FindChildByName(BodyRenderer.GameObject, boneName);

            if (_handBoneObject is not null && _handBoneObject.IsValid())
            {
                HandBoneName = boneName;

                if (DebugLogs)
                    Log.Info($"World weapon attached to fallback: {boneName}");

                return;
            }
        }

        if (DebugLogs)
            Log.Warning("Could not find right hand bone/target for world weapon.");
    }

    private GameObject FindChildByName(GameObject root, string name)
    {
        if (root is null)
            return null;

        foreach (var child in root.Children)
        {
            if (child.Name == name)
                return child;

            var found = FindChildByName(child, name);

            if (found is not null)
                return found;
        }

        return null;
    }
}