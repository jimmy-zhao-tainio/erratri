namespace Boolean;

public static class Classification
{
    public static PatchClassification Run(IntersectionSet set, TrianglePatchSet patches)
    {
        return PatchClassifier.Classify(set, patches);
    }
}
