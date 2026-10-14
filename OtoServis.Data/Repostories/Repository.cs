using Microsoft.EntityFrameworkCore;
using OtoServis.Data;
using System.Linq.Expressions;

namespace OtoServis.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ServisDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ServisDbContext context)
        {
            _context = context;
            // T tipi ne gelirse (Kullanici, Usta vs.), EF Core o tabloya (DbSet) bağlanır.
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            // Gelen şartı doğrudan Where içine koyup veritabanında filtreler
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // İlk parametre filtreleme (Where) için, ikinci parametre (params) ise Include'lar için
        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> filter = null,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            // 1. Eğer bir filtre (Where şartı) gönderildiyse önce onu uygula
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Eğer Include edilecek tablolar gönderildiyse, onları da sorguya ekle (JOIN)
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // Entity Framework'te FindAsync içine Include alamaz, 
            // bu yüzden FirstOrDefaultAsync kullanıyoruz:
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }
        public async Task AddAsync(T entity)  
        {
            // Sadece belleğe ekler
            await _dbSet.AddAsync(entity);
            
        }

        public async Task UpdateAsync(T entity)  //bellekte güncelleme yapar, veritabanına henüz yansıtmaz
        {
            _dbSet.Update(entity);
            
        }

        public async Task DeleteAsync(T entity)
        {
            // Nesneyi silinecek olarak işaretler
            _dbSet.Remove(entity);
            
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
        {
            // Sorguyu SQL Server'a "SELECT TOP 1 * FROM Tablo WHERE ..." olarak gönderir.
            // Bütün tabloyu çekmez, sadece eşleşen tek bir satırı getirir.
            return await _dbSet.FirstOrDefaultAsync(filter);
        }

        // BÜTÜN İŞLEMLERİ TOPLUCA SQL'E GÖNDEREN TEK METOT
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
