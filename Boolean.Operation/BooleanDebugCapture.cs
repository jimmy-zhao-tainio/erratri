namespace Boolean;

// Minimal debug capture for the last patch set assembled, gated by a debug flag.
public static class BooleanDebugCapture
{
    public static BooleanPatchSet? LastPatchSet { get; private set; }
    public static TrianglePatchSet? LastTrianglePatchSet { get; private set; }
    public static PatchClassification? LastClassification { get; private set; }
    public static IntersectionSet? LastIntersectionSet { get; private set; }
    public static BooleanOperationType LastOperation { get; private set; }

    public static void Capture(
        BooleanPatchSet patchSet,
        TrianglePatchSet trianglePatches,
        PatchClassification classification,
        IntersectionSet set,
        BooleanOperationType operation)
    {
        LastPatchSet = patchSet;
        LastTrianglePatchSet = trianglePatches;
        LastClassification = classification;
        LastIntersectionSet = set;
        LastOperation = operation;
    }

    public static void Clear()
    {
        LastPatchSet = null;
        LastTrianglePatchSet = null;
        LastClassification = null;
        LastIntersectionSet = null;
    }
}
