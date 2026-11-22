namespace Rihla.EditorTools
{
    /// <summary>
    /// Rihla's own scene-builder menus (Rihla &gt; Build ...) build Rihla's scenes from Rihla's files. In another game
    /// that uses the shared core they are greyed out, unless its CoreConfig turns them on.
    /// </summary>
    public static class RihlaMenus
    {
        public static bool Enabled => CoreConfig.Current.rihlaBuilderMenus;
    }
}
