using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using GalaSoft.MvvmLight;

namespace UIWPF.ViewModel
{
    public class ViewModelBaseExtended : ViewModelBase
    {
        protected bool SetProperty<T>(Expression<Func<T>> propertyExpression, ref T var, T value)
        {
            if (Equals(var, value)) return false;
            var = value;
            RaisePropertyChanged(propertyExpression);
            return true;
        }
    }
}
