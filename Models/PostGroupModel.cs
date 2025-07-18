using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARM.Models
{
    public class PostGroupModel
    {
        public int Side { get; set; }
        public ObservableCollection<PostModel> Posts { get; set; }
    }

}
