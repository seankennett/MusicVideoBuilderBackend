﻿using BuildEntities;

namespace BuilderEntities.Entities;

public class AssetsDownload
{
    public IEnumerable<string> LayerIds { get; set; }
    public List<string> TemporaryFiles { get; set; }
    public Resolution Resolution { get; set; }
}