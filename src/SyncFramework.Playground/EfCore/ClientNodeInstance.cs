namespace SyncFramework.Playground.EfCore
{
    public class ClientNodeInstance
    {
        public string Id { get; set; }
        public BlogsDbContext DbContext
        {
            get
            {
                return dbContext;
            }
            set
            {
                dbContext = value;
                this.SelectedBlog = dbContext.Blogs.FirstOrDefault();
            }
        }

        public string BlogName { get; set; }
        public int DeltaCount
        {
            get
            {
                var CountTask = DbContext.DeltaStore.GetDeltaCountAsync("", this.Id, default);
                CountTask.Wait();
                return CountTask.Result;
            }


        }
        Blog selectedBlog;
        private BlogsDbContext dbContext;

        public Blog SelectedBlog
        {
            get
            {
                return selectedBlog;
            }

            set
            {
                selectedBlog = value;
                SelectedBlogChanged(selectedBlog);
            }
        }
        public async Task AddBlog(string BlogName)
        {
            DbContext.Blogs.Add(new Blog { Name = BlogName });
            await DbContext.SaveChangesAsync();
            BlogName = string.Empty;
        }
        public List<Post> Posts { get; set; }
        public void SelectedBlogChanged(Blog Blog)
        {
            Posts = Blog?.Posts?.ToList();
        }
        public void OnRowClicked(object Blog)
        {
            //selectedOrderItem = selectItem; //StateHasChanged();
        }
    }
}
