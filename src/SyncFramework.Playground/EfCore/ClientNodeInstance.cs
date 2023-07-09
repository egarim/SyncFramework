namespace SyncFramework.Playground.EfCore
{
    public class ClientNodeInstance
    {
        public string Id { get; set; }
        public BlogsDbContext DbContext { get; set; }
   
        public int DeltaCount {
            get
            {
                var CountTask = DbContext.DeltaStore.GetDeltaCountAsync("", this.Id,default);
                CountTask.Wait();
                return CountTask.Result;
            }
          
        
        }
        public async Task AddBlog(string BlogName)
        {
            DbContext.Blogs.Add(new Blog { Name= BlogName });
            await DbContext.SaveChangesAsync();
        }
    }
}
