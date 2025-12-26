namespace Boolean;

public static class Selection
{
    public static BooleanPatchSet Run(BooleanOperationType operation, PatchClassification classification)
    {
        return BooleanClassification.Select(operation, classification);
    }
}
