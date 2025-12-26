namespace Boolean;

public static class Selection
{
    public static BooleanPatchSet Run(BooleanOperationType operation, PatchClassification classification)
    {
        return BooleanPatchClassifier.Select(operation, classification);
    }
}
