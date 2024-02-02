namespace MorganStanley.ComposeUI.ModuleLoader
{
    public static class ModuleInstanceExtensions
    {
        /// <summary>
        /// Gets the properties matching the specified type to the module instance
        /// </summary>
        /// <typeparam name="T">The type of the requested properties</typeparam>
        /// <returns>An enumerable of the properties matching or castable to the desired type</returns>
        public static IEnumerable<TProperty> GetProperties<TProperty>(this IModuleInstance thisObj)
        {
            return thisObj.GetProperties().OfType<TProperty>();
        }
    }
}
