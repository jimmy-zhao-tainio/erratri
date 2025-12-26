namespace Boolean;

// Minimal debug capture for the last patch set assembled, gated by a debug flag.
public static class BooleanDebugCapture
{
    public static BooleanPatchSet? LastPatchSet { get; private set; }
    public static TrianglePatches? LastTrianglePatches { get; private set; }
    public static PatchClassification? LastClassification { get; private set; }
    public static IntersectionSet? LastIntersectionSet { get; private set; }
    public static BooleanOperationType LastOperation { get; private set; }

    public static void Capture(
        BooleanPatchSet patchSet,
        TrianglePatches trianglePatches,
        PatchClassification classification,
        IntersectionSet set,
        BooleanOperationType operation)
    {
        LastPatchSet = patchSet;
        LastTrianglePatches = trianglePatches;
        LastClassification = classification;
        LastIntersectionSet = set;
        LastOperation = operation;
    }

    public static void Clear()
    {
        LastPatchSet = null;
        LastTrianglePatches = null;
        LastClassification = null;
        LastIntersectionSet = null;
    }
}
