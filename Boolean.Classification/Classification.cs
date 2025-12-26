namespace Boolean;

public static class Classification
{
    public static PatchClassification Run(IntersectionSet set, TrianglePatches patches)
    {
        return ClassificationCore.Run(set, patches);
    }
}
