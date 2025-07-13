using OnlineShopModular.Shared.Dtos.Identity;

using OnlineShopModular.Shared.Dtos.Todo;
using OnlineShopModular.Shared.Dtos.Products;
using OnlineShopModular.Shared.Dtos.Categories;
using Riok.Mapperly.Abstractions;
using OnlineShopModular.Shared.Dtos.Chatbot;

namespace OnlineShopModular.Shared;

/// <summary>
/// Patching methods help you patch the DTO you have received from the server (for example, after calling an Update api) 
/// onto the DTO you have bound to the UI. This way, the UI gets updated with the latest stored changes,
/// and there's no need to re-fetch that specific data from the server.
/// For complete end to end sample you can check ProfilePage.razor.cs
/// You can add as many as Patch methods you want for other DTO classes here.
/// For more information and to learn about customizing the mapping process, visit the website below:
/// https://mapperly.riok.app/docs/intro/
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial void Patch(this TodoItemDto source, TodoItemDto destination);
    public static partial void Patch(this ProductDto source, ProductDto destination);
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
    public static partial void Patch(this UserDto source, UserDto destination);
    public static partial void Patch(this EditUserRequestDto source, UserDto destination);
    public static partial void Patch(this UserDto source, EditUserRequestDto destination);
    public static partial void Patch(this SystemPromptDto source, SystemPromptDto destination);
}
