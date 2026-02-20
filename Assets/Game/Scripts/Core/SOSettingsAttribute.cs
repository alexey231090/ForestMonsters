using System;

[AttributeUsage(AttributeTargets.Class)]
public class SOSettingsAttribute : Attribute
{
    public string Category;
    public SOSettingsAttribute(string category = "General")
    {
        Category = category;
    }
}
