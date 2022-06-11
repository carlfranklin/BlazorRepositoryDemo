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

    public async Task<IEnumerable<TEntity>> GetAllAsync()
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

    public async Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Expression)
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

    public async Task<TEntity> GetByIdAsync(object id)
    {
        try
        {
            var idString = id.ToString();
            var len = idString.Length;
            var arg = WebUtility.HtmlEncode(idString);
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

    public async Task<TEntity> InsertAsync(TEntity entity)
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

    public async Task<TEntity> UpdateAsync(TEntity entityToUpdate)
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
    public async Task<bool> DeleteAsync(TEntity entityToDelete)
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
    public async Task<bool> DeleteByIdAsync(object id)
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

    public async Task DeleteAllAsync()
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