using Argon;

class ArrayIndexFilter : PathFilter
{
    public int? Index { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings? settings)
    {
        foreach (var t in current)
        {
            if (Index != null)
            {
                var v = GetTokenIndex(t, settings, Index.GetValueOrDefault());

                if (v != null)
                {
                    yield return v;
                }
            }
            else
            {
                if (t is JArray or JConstructor)
                {
                    foreach (var v in t)
                    {
                        yield return v;
                    }
                }
                else
                {
                    if (settings?.ErrorWhenNoMatch ?? false)
                    {
                        throw new JsonException($"Index * not valid on {t.GetType().Name}.");
                    }
                }
            }
        }
    }
}