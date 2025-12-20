using System;
using System.Collections.Generic;

namespace Pslg;

public readonly struct PslgFaceSelection
{
    public IReadOnlyList<PslgFace> InteriorFaces { get; }

    public PslgFaceSelection(IReadOnlyList<PslgFace> interiorFaces)
    {
        InteriorFaces = interiorFaces ?? throw new ArgumentNullException(nameof(interiorFaces));
    }
}
