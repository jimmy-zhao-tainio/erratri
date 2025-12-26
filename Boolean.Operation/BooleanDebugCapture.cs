namespace Boolean;

// Minimal debug capture for the last patch set assembled, gated by a debug flag.
public static class BooleanDebugCapture
{
    public static BooleanPatchSet? LastPatchSet { get; private set; }
    public static Patching? LastPatching { get; private set; }
    public static PatchClassification? LastClassification { get; private set; }
    public static IntersectionSet? LastIntersectionSet { get; private set; }
    public static BooleanOperationType LastOperation { get; private set; }

    public static void Capture(
        BooleanPatchSet patchSet,
        Patching trianglePatches,
        PatchClassification classification,
        IntersectionSet set,
        BooleanOperationType operation)
    {
        LastPatchSet = patchSet;
        LastPatching = trianglePatches;
        LastClassification = classification;
        LastIntersectionSet = set;
        LastOperation = operation;
    }

    public static void Clear()
    {
        LastPatchSet = null;
        LastPatching = null;
        LastClassification = null;
        LastIntersectionSet = null;
    }
}
