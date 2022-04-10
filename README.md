# Building Reusable Back-End Repositories

## Overview

In this module, we will create a hosted Blazor WebAssembly application with an API layer that uses the **repository pattern** to access two different data layers using a common interface, `IRepository`, which we will define. 

We will use `IRepository` on the server to access data, and also on the client to define a generic API Repository, which wraps calls to the API with an `HttpClient` object.

In fact, we will be using generics everywhere to build our classes. We want to avoid repeating boilerplate code for each entity we want to access, or data store.

We will start by making an in-memory repository, and then we will make a generic repository for accessing SQL databases using Entity Framework.

#### Create a Blazor WebAssembly App

Create a *hosted* Blazor WebAssembly application called *RepositoryDemo*. This will create three projects: *RepositoryDemo.Client*, *RepositoryDemo.Server*, and *RepositoryDemo.Shared*.

#### Install NewtonSoft.Json

Right-click on the Solution file, and select **Manage NuGet Packages for Solution...**

Browse for "Newtonsoft.Json" and install in both the Client and Server projects:

![image-20220322171923311](images/image-20220322171923311.png)

#### Add Models

To the *Shared* app, add a *Models* folder and add the following files to it:

*Customer.cs*

```c#
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
```

`Customer` will serve as our primary demo model. 

*APIEntityResponse.cs*

```c#
public class APIEntityResponse<TEntity> where TEntity : class
{
    public bool Success { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
    public TEntity Data { get; set; }
}
```

*APIListOfEntityResponse.cs*

```c#
public class APIListOfEntityResponse<TEntity> where TEntity : class
{
    public bool Success { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
    public IEnumerable<TEntity> Data { get; set; }
}
```

These two classes will be used as return types for our API controllers to add a little context to the actual entities returned.

*IRepository.cs*

```c#
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
#nullable disable
/// <summary>
/// Generic repository interface that uses
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<bool> Delete(TEntity EntityToDelete);
    Task<bool> Delete(object Id);
    Task DeleteAll(); // Be Careful!!!
    Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> Filter);
    Task<IEnumerable<TEntity>> GetAll();
    Task<TEntity> GetById(object Id);
    Task<TEntity> Insert(TEntity Entity);
    Task<TEntity> Update(TEntity EntityToUpdate);
}

/// <summary>
/// A serializable filter. An alternative to trying to serialize and deserialize LINQ expressions,
/// which are very finicky. This class uses standard types. 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class QueryFilter<TEntity> where TEntity : class
{
    /// <summary>
    /// If you want to return a subset of the properties, you can specify only
    /// the properties that you want to retrieve in the SELECT clause.
    /// Leave empty to return all columns
    /// </summary>
    public List<string> IncludePropertyNames { get; set; } = new List<string>();

    /// <summary>
    /// Defines the property names and values in the WHERE clause
    /// </summary>
    public List<FilterProperty> FilterProperties { get; set; } = new List<FilterProperty>();
    
    /// <summary>
    /// Specify the property to ORDER BY, if any 
    /// </summary>
    public string OrderByPropertyName { get; set; } = "";
    
    /// <summary>
    /// Set to true if you want to order DESCENDING
    /// </summary>
    public bool OrderByDescending { get; set; } = false;

    /// <summary>
    /// A custome query that returns a list of entities with the current filter settings.
    /// </summary>
    /// <param name="AllItems"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> GetFilteredList(List<TEntity> AllItems)
    {
        // Convert to IQueryable
        var query = AllItems.AsQueryable<TEntity>();

        // the expression will be used for each FilterProperty
        Expression<Func<TEntity, bool>> expression = null;

        // Process each property
        foreach (var filterProperty in FilterProperties)
        {
            // use reflection to get the property info
            PropertyInfo prop = typeof(TEntity).GetProperty(filterProperty.Name);

            // string
            if (prop.PropertyType == typeof(string))
            {
                if (filterProperty.Operator == FilterOperator.Equals)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower() == filterProperty.Value.ToString().ToLower();
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString() == filterProperty.Value.ToString();
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower() != filterProperty.Value.ToString().ToLower();
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString() != filterProperty.Value.ToString();
                else if (filterProperty.Operator == FilterOperator.StartsWith)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().StartsWith(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().StartsWith(filterProperty.Value.ToString());
                else if (filterProperty.Operator == FilterOperator.EndsWith)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().EndsWith(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().EndsWith(filterProperty.Value.ToString());
                else if (filterProperty.Operator == FilterOperator.Contains)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().Contains(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().Contains(filterProperty.Value.ToString());
            }
            // int
            if (prop.PropertyType == typeof(int))
            {
                int value = Convert.ToInt32(filterProperty.Value);

                if (filterProperty.Operator == FilterOperator.Equals)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) == value;
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) != value;
                else if (filterProperty.Operator == FilterOperator.LessThan)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) < value;
                else if (filterProperty.Operator == FilterOperator.GreaterThan)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) > value;
                else if (filterProperty.Operator == FilterOperator.LessThanOrEqual)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) <= value;
                else if (filterProperty.Operator == FilterOperator.GreaterThanOrEqual)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) >= value;
            }
            // datetime
            if (prop.PropertyType == typeof(DateTime))
            {
                DateTime value = DateTime.Parse(filterProperty.Value);

                if (filterProperty.Operator == FilterOperator.Equals)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) == value;
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) != value;
                else if (filterProperty.Operator == FilterOperator.LessThan)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) < value;
                else if (filterProperty.Operator == FilterOperator.GreaterThan)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) > value;
                else if (filterProperty.Operator == FilterOperator.LessThanOrEqual)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) <= value;
                else if (filterProperty.Operator == FilterOperator.GreaterThanOrEqual)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) >= value;
            }
            // Add expression creation code for other data types here.

            // apply the expression
            query = query.Where(expression);

        }

        // Include the specified properties
        foreach (var includeProperty in IncludePropertyNames)
        {
            query = query.Include(includeProperty);
        }

        // order by
        if (OrderByPropertyName != "")
        {
            PropertyInfo prop = typeof(TEntity).GetProperty(OrderByPropertyName);
            if (prop != null)
            {
                if (OrderByDescending)
                    query = query.Where(expression).OrderByDescending(x => prop.GetValue(x, null));
                else
                    query = query.Where(expression).OrderBy(x => prop.GetValue(x, null));
            }
        }

        // execute and return the list
        return query.ToList();
    }

}

/// <summary>
/// Defines a property for the WHERE clause
/// </summary>
public class FilterProperty
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public FilterOperator Operator { get; set; }
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Specify the compare operator
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    Contains,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual
}
```

The `IRepository<TEntity>` interface  will be used on the server as well as the client to ensure compatibility accessing data, no matter where the code resides.

This class file also includes code to describe custom queries that can easily be sent and received as json.

The `QueryFilter<TEntity>` can be used on the client as well as the server to define the same level of filter as using LINQ, except that it easily travels across the wire.

- `IncludedPropertyNames` defines the columns to return, ala the SELECT clause
- `FilterProperties` defines the properties to compare, ala the WHERE clause
- `OrderByPropertyName` defines the sort column, ala the ORDER BY clause
- `OrderByDescending` defines the direction of the sort, ala DESC
- The `GetFilteredList` method applies the current filter settings given a list of all items. While it's true that all of the items need to be loaded, what you give up in memory efficiency you gain in convenience. This method currently handles properties of type `string`, `int32` and `DateTime`.

The `FilterProperty` class defines columns to compare:

- `Name` is the Name of the property
- `Value` is a string representation of the value of the property
- `CaseSensitive` is a flag to determine whether case-sensitivity should be applied
- `FilterOerator` defines how to compare the column values (StartsWith, etc.)

#### Add Global Usings to Server

In the server project, add the following statements to the very top of *Program.cs*:

```c#
global using System.Linq.Expressions;
global using System.Reflection;
```

## Implement an In-Memory Repository

We're going to start by implementing an in-memory repository based on `IRepository`.  Once that is working we'll move on to using Entity Framework against a local SQL database.

To the server project, add a *Data* folder, and add this class to it:

*MemoryRepository.cs*

```c#
public class MemoryRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private List<TEntity> Data;
    private PropertyInfo IdProperty = null;
    private string IdPropertyName = "";
    public MemoryRepository(string idPropertyName)
    {
        IdPropertyName = idPropertyName;
        Data = new List<TEntity>();
        IdProperty = typeof(TEntity).GetProperty(idPropertyName);
    }

    public async Task<bool> Delete(TEntity EntityToDelete)
    {
        if (EntityToDelete == null) return false;


        await Task.Delay(0);
        try
        {
            if (Data.Contains(EntityToDelete))
            {
                lock (Data)
                {
                    Data.Remove(EntityToDelete);
                }
                return true;
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> Delete(object Id)
    {
        try
        {
            var EntityToDelete = await GetById(Id);
            return await Delete(EntityToDelete);


        }
        catch { }
        return false;
    }

    public async Task DeleteAll()
    {
        await Task.Delay(0);
        lock (Data)
        {
            Data.Clear();
        }
    }

    public async Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> JsonExpression)
    {
        
        var allitems = (await GetAll()).ToList();
        return await JsonExpression.GetFilteredList(allitems);
    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        await Task.Delay(0);
        return Data;
    }

    public async Task<TEntity> GetById(object Id)
    {
        await Task.Delay(0);
        if (IdProperty == null) return default(TEntity);
        TEntity entity = null;
        if (IdProperty.PropertyType.IsValueType)
        {
            entity = (from x in Data
                      where IdProperty.GetValue(x).ToString() == Id.ToString()
                      select x).FirstOrDefault();
        }
        else
        {
            entity = (from x in Data
                      where IdProperty.GetValue(x) == Id
                      select x).FirstOrDefault();
        }
        return entity;
    }


    public async Task<TEntity> Insert(TEntity Entity)
    {
        await Task.Delay(0);
        if (Entity == null) return default(TEntity);
        try
        {
            lock (Data)
            {
                Data.Add(Entity);
            }
            return Entity;
        }
        catch { }
        return default(TEntity);
    }


    public async Task<TEntity> Update(TEntity EntityToUpdate)
    {
        await Task.Delay(0);
        if (EntityToUpdate == null) return default(TEntity);
        if (IdProperty == null) return default(TEntity);
        try
        {
            var id = IdProperty.GetValue(EntityToUpdate);
            var entity = await GetById(id);
            if (entity != null)
            {
                lock (Data)
                {
                    var index = Data.IndexOf(entity);
                    Data[index] = EntityToUpdate;
                }
                return EntityToUpdate;
            }
            else
                return default(TEntity);
        }
        catch { }
        return default(TEntity);
    }
}
```

We're starting with a simple implementation of `IRepository<TEntity>` that simply stores data in memory. 

It is completely generic, meaning we can define one for any entity type. 

We're using a bit of Reflection to access the primary key property and get its value when we need to. Other than that, it's pretty straightforward. Take a moment to read through the code so you understand it.

#### Add MemoryRepository as a service

Add the following to the server project's *Startup.cs* file, in the `ConfigureServices` method:

```c#
builder.Services.AddSingleton<MemoryRepository<Customer>>(x =>
    new MemoryRepository<Customer>("Id"));
```

You might be wondering why I didn't define it as `IRepository<Customer>`. The reason is so we can differentiate it from another implementation of `IRepository<Customer>` which we will be adding next.

We're adding it as a Singleton because we only want one instance on the server shared between all clients.

In the above code we're configuring this manager telling it that the primary key property of `Customer` is named "Id."

#### Add an API Controller

To the server project's *Controllers* folder, add the following:

*InMemoryCustomersController.cs*

```c#
using Microsoft.AspNetCore.Mvc;

namespace RepositoryDemo.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InMemoryCustomersController : ControllerBase
    {
        MemoryRepository<Customer> customersManager;

        public InMemoryCustomersController(MemoryRepository<Customer> _customersManager)
        {
            customersManager = _customersManager;
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>> Get()
        {
            try
            {
                var result = await customersManager.GetAll();
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost("getwithfilter")]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>>
            GetWithFilter([FromBody] QueryFilter<Customer> Filter)
        {
            try
            {
                var result = await customersManager.Get(Filter);
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<APIEntityResponse<Customer>>> GetById(int Id)
        {
            try
            {
                var result = await customersManager.GetById(Id);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Customer Not Found" },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
         Insert([FromBody] Customer Customer)
        {
            try
            {
                var result = await customersManager.Insert(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after adding it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }


        [HttpPut]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
            Update([FromBody] Customer Customer)
        {
            try
            {
                var result = await customersManager.Update(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after updating it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<ActionResult<bool>> Delete(int Id)
        {
            try
            {
                return await customersManager.Delete(Id);
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }


        [HttpGet("deleteall")]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await customersManager.DeleteAll();
                return NoContent();
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }
    }
}
```

Because we added `MemoryDataManager<Customer>` as a singleton service, we can inject that right into our controller and use it to access the data store, in this case, an in-memory implementation of `IRepository<Customer>`

Note the use of `Task<ActionResult<T>>` in every endpoint. That's good practice. Also, any time we're returning an entity or list of entities we are wrapping the result in either `APIEntityResponse<T>` or `APIListOfEntityResponse<T>`. 

The `Get()` method has the most complex return type:

```c#
Task<ActionResult<APIListOfEntityResponse<Customer>>>
```

#### Add Global Usings to the Client 

Add the following statements to the very top of the Client project's *Program.cs*

```c#
global using System.Net.Http.Json;
global using Newtonsoft.Json;
global using System.Net;
global using System.Linq.Expressions;
```

#### Add an APIRepository class to the Client

To the client app, add a *Services* folder and add the following:

*APIRepository.cs*

```c#
using System.Linq.Expressions;
/// <summary>
/// Reusable API Repository base class that provides access to CRUD APIs
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class APIRepository<TEntity> : IRepository<TEntity>
  where TEntity : class
{
    string controllerName;
    string primaryKeyName;
    HttpClient http;

    public APIRepository(HttpClient _http,
      string _controllerName, string _primaryKeyName)
    {
        http = _http;
        controllerName = _controllerName;
        primaryKeyName = _primaryKeyName;
    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        try
        {
            var result = await http.GetAsync(controllerName);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            var response =
              JsonConvert.DeserializeObject<APIListOfEntityResponse<TEntity>>
               (responseBody);
            if (response.Success)
                return response.Data;
            else
                return new List<TEntity>();
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> Expression)
    {
        try
        {
            string url = $"{controllerName}/getwithfilter";
            var result = await http.PostAsJsonAsync(url, Expression);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            var response =
              JsonConvert.DeserializeObject<APIListOfEntityResponse<TEntity>>
               (responseBody);
            if (response.Success)
                return response.Data;
            else
                return new List<TEntity>();
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<TEntity> GetById(object id)
    {
        try
        {
            var arg = WebUtility.HtmlEncode(id.ToString());
            var url = controllerName + "/" + arg;
            var result = await http.GetAsync(url);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<APIEntityResponse<TEntity>>
               (responseBody);
            if (response.Success)
                return response.Data;
            else
                return null;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return null;
        }
    }

    public async Task<TEntity> Insert(TEntity entity)
    {
        try
        {
            var result = await http.PostAsJsonAsync(controllerName, entity);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<APIEntityResponse<TEntity>>
               (responseBody);
            if (response.Success)
                return response.Data;
            else
                return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<TEntity> Update(TEntity entityToUpdate)
    {
        try
        {
            var result = await http.PutAsJsonAsync(controllerName, entityToUpdate);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<APIEntityResponse<TEntity>>
               (responseBody);
            if (response.Success)
                return response.Data;
            else
                return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    public async Task<bool> Delete(TEntity entityToDelete)
    {
        try
        {
            var value = entityToDelete.GetType()
               .GetProperty(primaryKeyName)
               .GetValue(entityToDelete, null)
               .ToString();


            var arg = WebUtility.HtmlEncode(value);
            var url = controllerName + "/" + arg;
            var result = await http.DeleteAsync(url);
            result.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> Delete(object id)
    {
        try
        {
            var url = controllerName + "/" + WebUtility.HtmlEncode(id.ToString());
            var result = await http.DeleteAsync(url);
            result.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task DeleteAll()
    {
        try
        {
            var url = controllerName + "/deleteall";
            var result = await http.GetAsync(url);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {

        }
    }
}
```

`APIRepository<TEntity>` is a re-usable generic implementation of `IRepository<TEntity>` that we can use to create custom managers on the client side without having to rewrite the plumbing code for accessing every controller.

Take a look at the constructor. We're passing in an `HttpClient` with it's `BaseAddress` property already set, the controller name, and the primary key name.

```c#
    string controllerName;
    string primaryKeyName;
    HttpClient http;

    public APIRepository(HttpClient _http,
        string _controllerName, string _primaryKeyName)
    {
        http = _http;
        controllerName = _controllerName;
        primaryKeyName = _primaryKeyName;
    }
```

#### Create a Customer Repository on the client

To the client project's *Services* folder, add the following:

*CustomerRepository.cs*

```c#
public class CustomerRepoistory : APIRepository<Customer>
{
    HttpClient http;
    
    // swap out the controller name
    static string controllerName = "inmemorycustomers";
    //static string controllerName = "efcustomers";

    public CustomerRepoistory(HttpClient _http)
       : base(_http, controllerName, "Id")
    {
        http = _http;
    }
}
```

This is now how easy it is to add support on the client to access a new controller. In the constructor, we're just passing the http object, telling `APIRepository` that we'll be calling the `InMemoryCustomers`  controller, and that the primary key property name is "Id."

This is where you could implement additional methods in lieu of using the filtered `Get` method, such as a method to search customers by name.

#### Add a CustomerRepository service

To the client project's *Program.cs* file, add the following to `Main(string[] args)`:

```c#
builder.Services.AddScoped<CustomerRepoistory>();
```

#### Add using statement to *_Imports.razor*

```c#
@using RepositoryDemo.Shared
```

Adding these ensures we can access classes in these namespaces from .razor components.

#### Implement the Blazor code and markup

Change *\Pages\Index.razor* to the following:

```c#
@page "/"
@inject CustomerRepoistory CustomerManager

<h1>Repository Demo</h1>

@foreach (var customer in Customers)
{
    <p>(@customer.Id) @customer.Name, @customer.Email</p>
}

<button @onclick="UpdateIsadora">Update Isadora</button>
<button @onclick="DeleteRocky">Delete Rocky</button>
<button @onclick="DeleteHugh">Delete Hugh</button>
<button @onclick="GetJenny">GetJenny</button>
<button @onclick="AddCustomers">Reset Data</button>
<br />
<br />
<p>
    Search by Last Name: <input @bind=@SearchFilter />
    <button @onclick="Search">Search</button>
    <br />
    <br />
    <input style="zoom:1.5;" type="checkbox" @bind="CaseSensitive" /> Case Sensitive
    <br />
    <input style="zoom:1.5;" type="checkbox" @bind="Descending" /> Descending Order
    <br />
    <br />
    Options:<br />
    <select @onchange="SelectedOptionChanged" size=3 style="padding:10px;">
        <option selected value="StartsWith">Starts With</option>
        <option value="EndsWith">Ends With</option>
        <option value="Contains">Contains</option>
    </select>

</p>

<br />
<br />
<p>@JennyMessage</p>


@code
{
    List<Customer> Customers = new List<Customer>();
    string JennyMessage = "";
    string SearchFilter = "";
    bool CaseSensitive = false;
    bool Descending = false;
    FilterOperator filterOption = FilterOperator.StartsWith;

    void SelectedOptionChanged(ChangeEventArgs args)
    {
        switch (args.Value.ToString())
        {
            case "StartsWith":
                filterOption = FilterOperator.StartsWith;
                break;
            case "EndsWith":
                filterOption = FilterOperator.EndsWith;
                break;
            case "Contains":
                filterOption = FilterOperator.Contains;
                break;
        }
    }

    async Task Search()
    {
        try
        {
            var expression = new QueryFilter<Customer>();

            expression.FilterProperties.Add(new FilterProperty { Name = "Name", Value = SearchFilter, Operator = filterOption, CaseSensitive = CaseSensitive });
            expression.OrderByPropertyName = "Name";
            expression.OrderByDescending = Descending;

            //// Example, return where Id = 2
            //expression.FilterProperties.Add(new FilterProperty { Name = "Id", Value = "2", Operator = FilterOperator.Equals });
            //expression.OrderByPropertyName = "Name";
            //expression.OrderByDescending = Descending;

            //// Example, return where Id > 1
            //expression.FilterProperties.Add(new FilterProperty { Name = "Id", Value = "1", Operator = FilterOperator.GreaterThan });
            //expression.OrderByPropertyName = "Name";
            //expression.OrderByDescending = Descending;


            var list = await CustomerManager.Get(expression);
            Customers = list.ToList();

        }
        catch (Exception ex)
        {
            var msg = ex.Message;
        }
    }

    async Task DeleteRocky()
    {
        var rocky = (from x in Customers
                     where x.Email == "rocky@rhodes.com"
                     select x).FirstOrDefault();
        if (rocky != null)
        {
            await CustomerManager.Delete(rocky);
            await Reload();
        }
    }

    async Task DeleteHugh()
    {
        var hugh = (from x in Customers
                    where x.Email == "hugh@jass.com"
                    select x).FirstOrDefault();
        if (hugh != null)
        {
            await CustomerManager.Delete(hugh.Id);
            await Reload();
        }
    }

    async Task UpdateIsadora()
    {
        var isadora = (from x in Customers
                       where x.Email == "isadora@jarr.com"
                       select x).FirstOrDefault();
        if (isadora != null)
        {
            isadora.Email = "isadora@isadorajarr.com";
            await CustomerManager.Update(isadora);
            await Reload();
        }
    }

    async Task GetJenny()
    {
        JennyMessage = "";
        var jenny = (from x in Customers
                     where x.Email == "jenny@jones.com"
                     select x).FirstOrDefault();
        if (jenny != null)
        {
            var jennyDb = await CustomerManager.GetById(jenny.Id);
            if (jennyDb != null)
            {
                JennyMessage = $"Retrieved Jenny via Id {jennyDb.Id}";
            }
        }
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnInitializedAsync()
    {
        await AddCustomers();
    }

    async Task Reload()
    {
        JennyMessage = "";
        var list = await CustomerManager.GetAll();
        if (list != null)
        {
            Customers = list.ToList();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task AddCustomers()
    {
        await CustomerManager.DeleteAll();

        Customers.Clear();

        await CustomerManager.Insert(new Customer
            {
                Id = 1,
                Name = "Isadora Jarr",
                Email = "isadora@jarr.com"
            });

        await CustomerManager.Insert(new Customer
            {
                Id = 2,
                Name = "Rocky Rhodes",
                Email = "rocky@rhodes.com"
            });

        await CustomerManager.Insert(new Customer
            {
                Id = 3,
                Name = "Jenny Jones",
                Email = "jenny@jones.com"
            });

        await CustomerManager.Insert(new Customer
            {
                Id = 4,
                Name = "Hugh Jass",
                Email = "hugh@jass.com"
            });

        await Reload();
    }
}
```

This page exercises all features of `IRepository<TEntity>`: `GetAll`, `Get ` with a query filter,`GetById`, `Insert`, `Update`, and `Delete` two ways: by Id and by Entity.

![image-20220410180251563](images/image-20220410180251563.png)

The app displays four customers, their Ids, names, and email addresses.

If you press ![image-20210514130755412](images/image-20210514130755412.png)then Isadora's email address changes to **isadora@isadorajarr.com**:

​	![image-20210514130900651](images/image-20210514130900651.png)

If you press ![image-20210514130923188](images/image-20210514130923188.png)then Rocky will be deleted by entity:

![image-20210514131058298](images/image-20210514131058298.png)

If you press ![image-20210514131219347](images/image-20210514131219347.png)then Hugh will be deleted by Id:

![image-20210514131241975](images/image-20210514131241975.png)

If you press ![image-20210514131305800](images/image-20210514131305800.png)then Jenny's record will be retrieved using `GetById`

<img src="images/image-20220322175149382.png" alt="image-20220322175149382" style="zoom: 67%;" />

If you press ![image-20210514131416003](images/image-20210514131416003.png)or refresh the page the data will be reset to it's original state.

Try out the search functionality. 

![image-20220410180617860](images/image-20220410180617860.png)

## Implement an Entity Framework Repository

Now we're going to make a special Repository for working with Entity Framework. It will be generic, and will therefore be usable with any DbContext and Model.

#### Create the SQL Database

In Visual Studio, open the **SQL Server Object Explorer**, expand **MSSQLLocalDB**, right click on **Databases**, and select **Add New Database**. Name the new database *RepositoryDemo* and select the **OK** button.

![image-20220323174510706](images/image-20220323174510706.png)

Next, right-click on the **RepositoryDemo** database and select **New Query...**

![image-20220323174917490](images/image-20220323174917490.png)

Enter the following SQL:

```sql
CREATE TABLE [dbo].[Customer]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(50) NOT NULL, 
    [Email] NVARCHAR(50) NOT NULL
)
```

Press the green **Play** button to execute the statement

![image-20220323175142470](images/image-20220323175142470.png)

Double-click on the **RepositoryDemo.Server** project in the Solution Explorer to expose the *.csproj* file, and replace it with the following:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="6.0.3" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.3" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="6.0.3">
		  <PrivateAssets>all</PrivateAssets>
		  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
		<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="6.0.2" />
		<PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Client\RepositoryDemo.Client.csproj" />
		<ProjectReference Include="..\Shared\RepositoryDemo.Shared.csproj" />
	</ItemGroup>

</Project>
```

#### Scaffold an EF DbContext from the database

Open the **Package Manager Console** window, select the **RepositoryDemo.Server** project, and enter the following command:

```
Scaffold-DbContext "Server=(localdb)\mssqllocaldb;Database=RepositoryDemo;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Data
```

![image-20220323184417914](images/image-20220323184417914.png)



> **IMPORTANT**: Delete the *Customer.cs* file from the Server's *Data* folder. The app won't work if you don't delete it. 

![image-20220323200204764](images/image-20220323200204764.png)

#### Create the EF Repository

Add the following global using statement to the top of the Server project's *Program.cs* file:

```c#
global using Microsoft.EntityFrameworkCore;
```

To the *Data* folder, add a new class called *EFRepository.cs* :

```c#
public class EFRepository<TEntity, TDataContext> : IRepository<TEntity>
  where TEntity : class
  where TDataContext : DbContext
{
    protected readonly TDataContext context;
    internal DbSet<TEntity> dbSet;

    public EFRepository(TDataContext dataContext)
    {
        context = dataContext;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        dbSet = context.Set<TEntity>();
    }
    public async Task<IEnumerable<TEntity>> GetAll()
    {
        await Task.Delay(0);
        return dbSet;
    }

    public async Task<TEntity> GetById(object Id)
    {
        return await dbSet.FindAsync(Id);
    }

    public async Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> Filter)
    {
        var allitems = (await GetAll()).ToList();
        return await Filter.GetFilteredList(allitems);
    }

    public async Task<TEntity> Insert(TEntity entity)
    {
        await dbSet.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<TEntity> Update(TEntity entityToUpdate)
    {
        var dbSet = context.Set<TEntity>();
        dbSet.Attach(entityToUpdate);
        context.Entry(entityToUpdate).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return entityToUpdate;
    }

    public async Task<bool> Delete(TEntity entityToDelete)
    {
        if (context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
        return await context.SaveChangesAsync() >= 1;
    }

    public async Task<bool> Delete(object id)
    {
        TEntity entityToDelete = await dbSet.FindAsync(id);
        return await Delete(entityToDelete);
    }

    public async Task DeleteAll()
    {
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Customer");
    }
}
```

#### Add an EF Customer API Controller

To the *Controllers* folder, add *EFCustomersController.cs* :

```c#
using Microsoft.AspNetCore.Mvc;
using RepositoryDemo.Server.Data;


namespace RepositoryDemo.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EFCustomersController : ControllerBase
    {
        EFRepository<Customer, RepositoryDemoContext> customersManager;

        public EFCustomersController(EFRepository<Customer, RepositoryDemoContext> _customersManager)
        {
            customersManager = _customersManager;
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>> Get()
        {
            try
            {
                var result = await customersManager.GetAll();
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost("getwithfilter")]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>> 
            GetWithFilter([FromBody] QueryFilter<Customer> Filter)
        {
            try
            {
                var result = await customersManager.Get(Filter);
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result.ToList()
                });
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<APIEntityResponse<Customer>>> GetById(int Id)
        {
            try
            {
                var result = await customersManager.GetById(Id);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Customer Not Found" },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
         Insert([FromBody] Customer Customer)
        {
            try
            {
                Customer.Id = 0; // Make sure you do this!
                var result = await customersManager.Insert(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after adding it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPut]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
         Update([FromBody] Customer Customer)
        {
            try
            {
                var result = await customersManager.Update(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after updating it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<ActionResult<bool>> Delete(int Id)
        {
            try
            {
                return await customersManager.Delete(Id);
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

        [HttpGet("deleteall")]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await customersManager.DeleteAll();
                return NoContent();
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }
    }
}
```

#### Add Services to the Server app

Add the following lines to the *Program.cs* file:

```c#
builder.Services.AddTransient<RepositoryDemoContext, RepositoryDemoContext>();
builder.Services.AddTransient<EFRepository<Customer, RepositoryDemoContext>>();
```

You'll need this:

```c#
using RepositoryDemo.Server.Data;
```

The `RepositoryDemoContext` and `EFRepository` need to be defined as transient services, because the controller is transient also. 

#### Modify the Client ever so slightly

Change *\Services\CustomerRepository.cs* to point to the EF Controller:

```c#
public class CustomerRepoistory : APIRepository<Customer>
{
    HttpClient http;
    
    // swap out the controller name
    //static string controllerName = "inmemorycustomers";
    static string controllerName = "efcustomers";

    public CustomerRepoistory(HttpClient _http)
       : base(_http, controllerName, "Id")
    {
        http = _http;
    }
}
```

#### Run the app

Give it a little time to bring the database up and generate the initial records. It will look exactly the same as when we were using the In Memory repository:

![image-20220410180251563](images/image-20220410180251563.png)

After running the app, take a look at the database by right-clicking on the **Customer** table in the **SQL Server Object Explorer** and selecting **View Data**:

![image-20220323191200976](images/image-20220323191200976.png)

That's it!
