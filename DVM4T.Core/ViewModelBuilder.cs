﻿using DVM4T.Attributes;
using DVM4T.Contracts;
using DVM4T.Exceptions;
using DVM4T.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DVM4T.Core
{
    /// <summary>
    /// Core implementation of View Model Builder 
    /// <remarks>
    /// If using View Model Type inference, this should be used as a singleton due to the reflection overhead 
    /// involved in LoadViewModels.
    /// </remarks>
    /// </summary>
    public class ViewModelBuilder : IViewModelBuilder
    {
        private IDictionary<ViewModelAttribute, Type> viewModels = new Dictionary<ViewModelAttribute, Type>();
        private IList<Assembly> loadedAssemblies = new List<Assembly>();
        public IViewModelKeyProvider keyProvider;
        /// <summary>
        /// New View Model Builder
        /// </summary>
        /// <param name="keyProvider">A View Model Key provider</param>
        public ViewModelBuilder(IViewModelKeyProvider keyProvider)
        {
            if (keyProvider == null) throw new ArgumentNullException("keyProvider");
            this.keyProvider = keyProvider;
        }
        #region IViewModelBuilder
        public IViewModelKeyProvider ViewModelKeyProvider { get { return keyProvider; } }
        /// <summary>
        /// Loads View Model Types from an Assembly. Use minimally due to reflection overhead.
        /// </summary>
        /// <param name="assembly"></param>
        public void LoadViewModels(Assembly assembly) //We assume we have a singleton of this instance, otherwise we incur a lot of overhead
        {
            if (!loadedAssemblies.Contains(assembly))
            {
                loadedAssemblies.Add(assembly);
                ViewModelAttribute viewModelAttr;
                foreach (var type in assembly.GetTypes())
                {
                    viewModelAttr = ReflectionUtility.ReflectionCache.GetCustomAttribute<ViewModelAttribute>(type);
                    if (viewModelAttr != null && !viewModels.ContainsKey(viewModelAttr))
                    {
                        viewModels.Add(viewModelAttr, type);
                    }
                }
            }
        }
        public IComponentPresentationViewModel BuildCPViewModel(Type type, IComponentPresentation cp)
        {
            IComponentPresentationViewModel viewModel = null;
            viewModel = (IComponentPresentationViewModel)ReflectionUtility.ReflectionCache.CreateInstance(type);
            viewModel.ComponentPresentation = cp;
            viewModel.Builder = this;
            IFieldSet fields = cp.Component.Fields;
            ProcessFields(fields, viewModel, type, cp.ComponentTemplate, cp.Component.MetadataFields);
            return viewModel;
        }
        public T BuildCPViewModel<T>(IComponentPresentation cp) where T : class, IComponentPresentationViewModel
        {
            Type type = typeof(T);
            return (T)BuildCPViewModel(type, cp);
        }
        public IComponentPresentationViewModel BuildCPViewModel(IComponentPresentation cp)
        {
            if (cp == null) throw new ArgumentNullException("cp");
            var key = new ViewModelAttribute(cp.Component.Schema.Title, false)
            {
                ViewModelKeys = GetViewModelKey(cp.ComponentTemplate)
            };
            IComponentPresentationViewModel result = null;
            //var type = viewModels.Where(x => x.Key.Equals(key)).Select(x => x.Value).FirstOrDefault();
            Type type = viewModels.ContainsKey(key) ? viewModels[key] : null; //Keep an eye on this -- GetHashCode isn't the same as Equals
            if (type != null)
            {
                result = (IComponentPresentationViewModel)BuildCPViewModel(type, cp);
            }
            else
            {
                throw new ViewModelTypeNotFoundExpception(key.SchemaName, key.ViewModelKeys.FirstOrDefault());
            }
            return result;
        }
        public T BuildEmbeddedViewModel<T>(IFieldSet embeddedFields, IComponentTemplate template) where T : class, IEmbeddedSchemaViewModel
        {
            Type type = typeof(T);
            return (T)BuildEmbeddedViewModel(type, embeddedFields, template);
        }
        public IEmbeddedSchemaViewModel BuildEmbeddedViewModel(IFieldSet embeddedFields, ISchema schema, IComponentTemplate template)
        {
            if (embeddedFields == null) throw new ArgumentNullException("embeddedFields");
            if (schema == null) throw new ArgumentNullException("schema");
            if (template == null) throw new ArgumentNullException("template");
            var key = new ViewModelAttribute(schema.Title, false)
            {
                ViewModelKeys = GetViewModelKey(template)
            };
            IEmbeddedSchemaViewModel result = null;
            //var type = viewModels.Where(x => x.Key.Equals(key)).Select(x => x.Value).FirstOrDefault();
            Type type = viewModels.ContainsKey(key) ? viewModels[key] : null; //Keep an eye on this -- GetHashCode isn't the same as Equals
            if (type != null)
            {
                result = (IEmbeddedSchemaViewModel)BuildEmbeddedViewModel(type, embeddedFields, template);
            }
            else
            {
                throw new ViewModelTypeNotFoundExpception(key.SchemaName, key.ViewModelKeys.FirstOrDefault());
            }
            return result;
        }
        public IEmbeddedSchemaViewModel BuildEmbeddedViewModel(Type type, IFieldSet embeddedFields, IComponentTemplate template)
        {
            IEmbeddedSchemaViewModel viewModel = (IEmbeddedSchemaViewModel)ReflectionUtility.ReflectionCache.CreateInstance(type);
            viewModel.Fields = embeddedFields;
            viewModel.ComponentTemplate = template;
            viewModel.Builder = this;
            ProcessFields(embeddedFields, viewModel, type, template);
            return viewModel;
        }
        #endregion

        #region Private methods
        private void ProcessFields(IFieldSet contentFields, object viewModel, Type type, IComponentTemplate template, IFieldSet metadataFields = null)
        {
            //PropertyInfo[] props = type.GetProperties();
            var props = ReflectionUtility.ReflectionCache.GetFieldProperties(type);
            IField field;
            IFieldSet fields;
            string fieldName;
            IFieldAttribute fieldAttribute;
            object fieldValue = null;
            foreach (var prop in props)
            {
                fieldAttribute = prop.FieldAttribute;//prop.GetCustomAttributes(typeof(FieldAttributeBase), true).FirstOrDefault() as FieldAttributeBase;
                if (fieldAttribute != null) //It has a FieldAttribute
                {
                    fieldName = fieldAttribute.FieldName;
                    fields = fieldAttribute.IsMetadata ? metadataFields : contentFields;
                    if (fields != null && fields.ContainsKey(fieldName))
                    {
                        //TODO: Check the property type and make sure it matches expected return type or throw an exception -- not sure this is worth it
                        field = fields[fieldName];
                        if (fields != null)
                        {
                            fieldValue = fieldAttribute.GetFieldValue(field, prop.PropertyType, template, this); //delegate all the real work to the Field Attribute object itself. Allows for custom attribute types to easily be added
                            if (fieldValue != null)
                            {
                                try
                                {
                                    prop.Set(viewModel, fieldValue);
                                }
                                catch (Exception e)
                                {
                                    if (e is TargetException || e is InvalidCastException)
                                        throw new FieldTypeMismatchException(prop, fieldAttribute, fieldValue);
                                    else throw e;
                                }
                            }
                        }
                    }
                }
            }
        }
        private string[] GetViewModelKey(IComponentTemplate template)
        {
            string key = keyProvider.GetViewModelKey(template);
            return String.IsNullOrEmpty(key) ? null : new string[] { key };
        }
        #endregion
    }

}
