using System;

// Create several singletons that contain our big GPO data blob so we can access it without reparsing it.
public static class JankyDb
{
    private static JObject _instance;

    public static JObject Instance => _instance ?? (_instance = JObject.Parse(Resources.PolData));
}
