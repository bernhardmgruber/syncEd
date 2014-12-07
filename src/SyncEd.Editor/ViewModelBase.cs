using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SyncEd.Editor
{
    public abstract class ViewModelBase
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            ValidatePropertyName(propertyName);
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void SetProperty<T>(ref T backingField, T newValue, [CallerMemberName]string propertyName = null)
        {
            if (!Equals(backingField, newValue)) {
                backingField = newValue;

                OnPropertyChanged(propertyName);
            }
        }

        protected void SetProperty<T>(ref T backingField, T newValue, Expression<Func<T>> propertyExpression)
        {
            if (!Equals(backingField, newValue)) {
                backingField = newValue;

                OnPropertyChanged(GetPropertyNameFromExpression(propertyExpression));
            }
        }

        private void ValidatePropertyName(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null) {
                throw new ArgumentException("Invalid property propertyName: " + propertyName);
            }
        }

        public static string GetPropertyNameFromExpression<T>(Expression<Func<T>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;

            if (lambda.Body is UnaryExpression) {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            } else {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member.Name;
        }
    }
}
