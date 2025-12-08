using SqlKata;

namespace Demo.Infra.Repositories;

public static class CompanyQueryBuilder
{
    public static Query? Insert(string name)
    {
        Query? query = new Query("companies")
            .AsInsert(new { company_name = name });
        return query;
    }
    
    public static Query? SelectAll()
    {
        Query? query = new Query("companies")
            .Select("company_id", "company_name");
        return query;
    }
    
    public static Query? SelectById(int id)
    {
        Query? query = new Query("companies")
            .Select("company_id", "company_name")
            .Where("company_id", id);
        return query;
    }

    public static Query? Select(string? searchTerm, int page, int pageSize)
    {
        Query? query = new Query("companies")
            .Select("company_id", "company_name")
            .OrderBy("company_id")
            .Offset(page  * pageSize)
            .Limit(pageSize);
        
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query.WhereLike("company_name", $"%{searchTerm}%");
        }
        
        return query;
    }
    
    public static Query? Count(string? searchTerm)
    {
        Query? query = new Query("companies")
            .AsCount();
        
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query.WhereLike("company_name", $"%{searchTerm}%");
        }
        
        return query;
    }
    
    public static Query? UpdateById(int id, string name)
    {
        Query? query = new Query("companies")
            .AsUpdate(new { company_name = name })
            .Where("company_id", id);
        return query;
    }
    
    public static Query? DeleteById(int id)
    {
        Query? query = new Query("companies")
            .AsDelete()
            .Where("company_id", id);
        return query;
    }
    
    public static Query? DeleteAll()
    {
        Query? query = new Query("companies")
            .AsDelete();
        return query;
    }
    
    public static Query? SelectByName(string name)
    {
        Query? query = new Query("companies")
            .Select("company_id", "company_name")
            .Where("company_name", name);
        return query;
    }
    
    public static Query? UpdateByName(string name, string newName)
    {
        Query? query = new Query("companies")
            .AsUpdate(new { company_name = newName })
            .Where("company_name", name);
        return query;
    }
    
    public static Query? DeleteByName(string name)
    {
        Query? query = new Query("companies")
            .AsDelete()
            .Where("company_name", name);
        return query;
    }
}
