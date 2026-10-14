using System.Linq.Expressions;

namespace OtoServis.Data.Repositories
{
    // <T> ifadesi bunun jenerik olduğunu gösterir. T, herhangi bir veritabanı modeli (Entity) olabilir.
    // "where T : class" kısıtlaması, T'nin sadece bir sınıf (örn: Musteri, Usta) olabileceğini belirtir.
    public interface IRepository<T> where T : class
    {
        // Tüm verileri çekerken Include yapabilmek için:
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> filter = null, params Expression<Func<T, object>>[] includes);

        // Geriye dönük kodlarının (Where filtrelerinin) bozulmaması için:
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);

        // Tek bir veriyi (Id ile) çekerken Include yapabilmek için:
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
       
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);

        // YENİ EKLENECEK METOT: İçine lambda (x => x.Id == 1) yazabilmemizi sağlar
        Task<T> GetAsync(Expression<Func<T, bool>> filter);

        Task SaveChangesAsync();
    }
}
