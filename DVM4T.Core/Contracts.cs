﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace DVM4T.Contracts
{
    #region XPathable Data - probably won't use
    public interface IXPathableNode
    {
        IXPathableNode SelectSingleNode(string xpath);
        IXPathableNode[] SelectNodes(string xpath);
        string Name { get; }
        string Value { get; }
        IAttribute[] Attributes { get; }
        IXPathableNode ParentNode { get; }
        IXPathableNode[] GetAllChildNodes(); //necessary?
        string GetAsXmlString();
    }
    //Need to be able to retrieve Attributes via XPath as well
    public interface IAttribute
    {
        string Key { get; set; }
        string Value { get; set; }
    }

    public interface IHaveNode
    {
        IXPathableNode Node { get; set; }
    }
    //Is this interface necessary for this implementation? Seems to be connected to specifically an XML implementation
    public interface ITridionXPathProvider
    {
        string GetFieldXPath(string fieldName, int index);

        //Are any of these necessary?
        string GetComponentXPath();
        string GetComponentTemplateXPath();
        string GetFieldSetXPath(string name); //necessary?
        string GetSchemaXPath();
    }

    #endregion

    #region Tridion interfaces


    public interface IHaveData //consider making this generic i.e. IHaveData<T> { T BaseData { get; } } -- would require a lot of refactoring
    {
        object BaseData { get; }
    }

    public interface ITridionItemData : IHaveData
    {
        string TcmUri { get; }
        string Title { get; }
    }
    public interface IContentData : ITemplatedViewModelData
    {
        IFieldsData Content { get; }
        ISchemaData Schema { get; }
    }
    public interface IPageData : ITridionItemData, ITemplatedViewModelData
    {
        IList<IContentPresentationData> ComponentPresentations { get; }
        string FileName { get; }
    }   
    public interface IContentPresentationData : ITridionItemData, IContentData //This is essentially a replacement for IComponent
    {
        IComponentData Component { get; }
        IMultimediaData MultimediaData { get; }
    }

    //Deprecated?? - use IContentPresentationData instead? 
    public interface IComponentData : ITridionItemData, IViewModelData 
    {
        IFieldsData Content { get; }
        ISchemaData Schema { get; }
        IMultimediaData MultimediaData { get; }
    }
    public interface ITemplateData : ITridionItemData, IViewModelData
    {
        DateTime RevisionDate { get; }
    }

    public interface IMultimediaData : IHaveData
    {
        string Url { get; }
        string FileName { get; }
        string MimeType { get; }
    }

    public interface IFieldsData : IHaveData
    {
        IFieldData this[string key] { get; } //Convert this to xpath
        bool ContainsKey(string key); //check if the XPath exists
    }

    public interface ISchemaData : ITridionItemData
    {
        string RootElementName { get; }
    }
    public interface ICanUseXpm
    {
        string XPath { get; }
        string Name { get; }
        string RootElementName { get; }
    }
    public interface IFieldData : ICanUseXpm, IHaveData
    {
        //The object Value is an important part of this framework -- this object will be retrieved based on the FieldAttribute's overriden method. Implementer could alternatively use the BaseData object to circumvent
        /// <summary>
        /// A set of Values representing the underlying value of this Field
        /// </summary>
        IEnumerable Values { get; } //should this be object or is it ok as Enumerable?
        ISchemaData EmbeddedSchema { get; }
        /// <summary>
        /// The Field Type in String form
        /// </summary>
        string FieldTypeString { get; } //maybe make an enum?? This would prescribe the implementation though -- could use object to make it really generic
        /// <summary>
        /// The Keyword(s) for this Field if it is a Keyword Field. Otherwise should be null or empty.
        /// </summary>
        IList<IKeywordData> Keywords { get; }
    }

    public interface IKeywordData : ITridionItemData, IViewModelData
    {
        string Key { get; }
    }

    #endregion

    /// <summary>
    /// A View Model
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// Consolidated data for this View Model
        /// </summary>
        IViewModelData ModelData { get; set; }
    }

    /// <summary>
    /// View Model Data - the basic data that all View Models have
    /// </summary>
    public interface IViewModelData : IHaveData //Should this extend IHaveData? I don't think so at this time because each piece i.e. Content, MetadataFields has their own BaseData object
    {
        /// <summary>
        /// The underlying data object that the View Model represents
        /// </summary>
        //IViewModelFactory Builder { get; } //This isn't actually needed anywhere
        /// <summary>
        /// Metadata for the View Model
        /// </summary>
        IFieldsData Metadata { get; }
        /// <summary>
        /// Template for the View Model
        /// </summary>
        //ITemplateData Template { get; } //This might present a problem with creating a Model for Keywords . . . there is no template
        /// <summary>
        /// Publication ID of the underlying Tridion item
        /// </summary>
        int PublicationId { get; }
    }

    public interface ITemplatedViewModelData : IViewModelData
    {
        /// <summary>
        /// Template for the View Model
        /// </summary>
        ITemplateData Template { get; } 
    }

    public interface IViewModelResolver
    {
        /// <summary>
        /// Resolves an instance of a View Model of the given Type
        /// </summary>
        /// <param name="type">View Model Type to resolve</param>
        /// <param name="data">View Model Data for context</param>
        /// <returns>An instance of a View Model of the input Type</returns>
        IViewModel ResolveModel(Type type, IViewModelData data);
        /// <summary>
        /// Gets a list of Model Property objects for all Properties marked with a IPropertyAttribute Attribute for the given Type
        /// </summary>
        /// <param name="type">Type with the Properties to search</param>
        /// <returns>List of Model Properties</returns>
        IList<IModelProperty> GetModelProperties(Type type);
        /// <summary>
        /// Gets a specfic Model Attribute in the given Type
        /// </summary>
        /// <typeparam name="T">Type of Model Attribute to look for</typeparam>
        /// <param name="type">Type to search</param>
        /// <returns>A Model Attribute</returns>
        T GetCustomAttribute<T>(Type type) where T : IModelAttribute;

        //Possible to do something like this? How to make the parameters generic?
        //T GetModelData<T>(data parameters) where T : IViewModelData
    }
    /// <summary>
    /// A suggested interface for generating View Model Data
    /// </summary>
    public interface IModelDataFactory
    {
        T GetModelData<T>(params object[] data) where T : IViewModelData;
    }

    public interface IViewModelFactory
    {
        /// <summary>
        /// Loads all View Model classes from an assembly
        /// </summary>
        /// <param name="assembly">The Assembly with the view model Types to load</param>
        /// <remarks>
        /// Required for use of builder methods that don't require a Type parameter or generic.
        /// The Builder will only use Types tagged with the ViewModelAttribute class.
        /// </remarks>
        void LoadViewModels(Assembly assembly);
        /// <summary>
        /// Finds a View Model with the specified Type using the input Data
        /// </summary>
        /// <typeparam name="T">Type of View Model Attribute</typeparam>
        /// <param name="data">View Model Data to search for</param>
        /// <param name="typesToSearch">Optional array of possible Types to search through</param>
        /// <returns></returns>
        Type FindViewModelByAttribute<T>(IViewModelData data, Type[] typesToSearch = null) where T : IModelAttribute;
        IViewModel BuildViewModel(IViewModelData modelData);
        IViewModel BuildViewModelByAttribute<T>(IViewModelData modelData) where T : IModelAttribute;
        IViewModel BuildViewModel(Type type, IViewModelData modelData);
        T BuildViewModel<T>(IViewModelData modelData) where T : IViewModel;
    }

    [Obsolete("Use IViewModelFactory instead")]
    public interface IViewModelBuilder
    {
        /// <summary>
        /// Finds a View Model with the specified Type using the input Data
        /// </summary>
        /// <typeparam name="T">Type of View Model Attribute</typeparam>
        /// <param name="data">View Model Data to search for</param>
        /// <param name="typesToSearch">Optional array of possible Types to search through</param>
        /// <returns></returns>
        Type FindViewModelByAttribute<T>(IViewModelData data, Type[] typesToSearch = null) where T : IModelAttribute;
        /// <summary>
        /// Builds a View Model from previously loaded Assemblies using the input Component Presentation. This method infers the View Model type by comparing
        /// information from the Component Presentation object to the attributes of the View Model classes.
        /// </summary>
        /// <param name="cp">Component Presentation object</param>
        /// <remarks>
        /// The LoadViewModels method must be called with the desired View Model Types in order for this to return a valid object.
        /// </remarks>
        /// <returns>Component Presentation View Model</returns>
        IViewModel BuildCPViewModel(IContentPresentationData componentPresentation); //A way to build view model without passing type -- type is inferred using loaded assemblies
        /// <summary>
        /// Builds a View Model from a FieldSet using the schema determine the View Model class to use.
        /// </summary>
        /// <param name="embeddedFields">Embedded FieldSet</param>
        /// <param name="embeddedSchema">Embedded Schema</param>
        /// <param name="template">Component Template to use for generating XPM Markup for any linked components</param>
        /// <returns>Embedded Schema View Model</returns>
        IViewModel BuildEmbeddedViewModel(IFieldsData embeddedFields, ISchemaData embeddedSchemaTitle, ITemplateData template);
        /// <summary>
        /// Builds a View Model from a Component Presentation using the type parameter to determine the View Model class to use.
        /// </summary>
        /// <param name="type">Type of View Model class to return</param>
        /// <param name="cp">Component Presentation</param>
        /// <returns>Component Presentation View Model</returns>
        IViewModel BuildCPViewModel(Type type, IContentPresentationData componentPresentation);
        /// <summary>
        /// Builds a View Model from a FieldSet using the generic type to determine the View Model class to use.
        /// </summary>
        /// <param name="type">Type of View Model class to return</param>
        /// <param name="embeddedFields">Embedded FieldSet</param>
        /// <param name="template">Component Template to use for generating XPM Markup for any linked components</param>
        /// <returns>Embedded Schema View Model</returns>
        IViewModel BuildEmbeddedViewModel(Type type, IFieldsData embeddedFields, ISchemaData schema, ITemplateData template);
        /// <summary>
        /// Builds a View Model from a Component Presentation using the generic type to determine the View Model class to use.
        /// </summary>
        /// <typeparam name="T">Type of View Model class to return</typeparam>
        /// <param name="cp">Component Presentation</param>
        /// <returns>Component Presentation View Model</returns>
        T BuildCPViewModel<T>(IContentPresentationData componentPresentation) where T : class, IViewModel;
        /// <summary>
        /// Builds a View Model from a FieldSet using the generic type to determine the View Model class to use.
        /// </summary>
        /// <typeparam name="T">Type of View Model class to return</typeparam>
        /// <param name="embeddedFields">Embedded FieldSet</param>
        /// <param name="template">Component Template to use for generating XPM Markup for any linked components</param>
        /// <returns>Embedded Schema View Model</returns>
        T BuildEmbeddedViewModel<T>(IFieldsData embeddedFields, ISchemaData schema, ITemplateData template) where T : class, IViewModel;
        /// <summary>
        /// The View Model Key Provider for this Builder
        /// </summary>
        IViewModelKeyProvider ViewModelKeyProvider { get; }
        /// <summary>
        /// Builds a View Model for a Page using the input data
        /// </summary>
        /// <typeparam name="T">Type of View Model</typeparam>
        /// <param name="page">Page Data</param>
        /// <returns>View Model for the Page</returns>
        T BuildPageViewModel<T>(IPageData page) where T : IViewModel;
        /// <summary>
        /// Builds a View Model for a Page using the input data
        /// </summary>
        /// <param name="type">Type of View Model</param>
        /// <param name="page">Page Data</param>
        /// <returns>View Model for the Page</returns>
        IViewModel BuildPageViewModel(Type type, IPageData page);
        /// <summary>
        /// Builds a View Model for a Page using the input data
        /// </summary>
        /// <param name="page">Page Data</param>
        /// <returns>View Model for the Page</returns>
        IViewModel BuildPageViewModel(IPageData page);
        /// <summary>
        /// Loads all View Model classes from an assembly
        /// </summary>
        /// <param name="assembly">The Assembly with the view model Types to load</param>
        /// <remarks>
        /// Required for use of builder methods that don't require a Type parameter or generic.
        /// The Builder will only use Types tagged with the ViewModelAttribute class.
        /// </remarks>
        void LoadViewModels(Assembly assembly);
    }

    public interface IXpmMarkupService
    {
        /// <summary>
        /// Renders the XPM Markup for a field
        /// </summary>
        /// <param name="field">Tridion Field</param>
        /// <param name="index">Optional index for multi value fields</param>
        /// <returns>XPM Markup</returns>
        string RenderXpmMarkupForField(IFieldData field, int index = -1);
        /// <summary>
        /// Renders the XPM Markup for a Component Presentation
        /// </summary>
        /// <param name="cp">Component Presentation</param>
        /// <param name="region">Optional region</param>
        /// <returns>XPM Markup</returns>
        string RenderXpmMarkupForComponent(IContentPresentationData cp, string region = null);
        /// <summary>
        /// Determines if Site Edit is enabled for a particular item
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns></returns>
        bool IsSiteEditEnabled(ITridionItemData item);
        /// <summary>
        /// Determines if Site Edit is enabeld for a publication
        /// </summary>
        /// <param name="publicationId">Publication ID</param>
        /// <returns></returns>
        bool IsSiteEditEnabled(int publicationId);
    }

    //TODO: Instantiate and add these to the IViewModel using System.Reflection's MakeGenericType
    public interface IXpmRenderer<TModel> where TModel : IViewModel
    {
        /// <summary>
        /// Renders both XPM Markup and Field Value 
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup and field value</returns>
        HtmlString XpmEditableField<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1);
        /// <summary>
        /// Renders both XPM Markup and Field Value for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmEditableField(m => m.Content, content);
        /// }
        /// </example>
        /// <returns>XPM Markup and field value</returns>
        HtmlString XpmEditableField<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item);
        /// <summary>
        /// Renders the XPM markup for a field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup</returns>
        HtmlString XpmMarkupFor<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1);
        /// <summary>
        /// Renders XPM Markup for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmMarkupFor(m => m.Content, content);
        ///     @content;
        /// }
        /// </example>
        /// <returns>XPM Markup</returns>
        HtmlString XpmMarkupFor<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item);
        /// <summary>
        /// Renders the XPM Markup for a Component Presentation
        /// </summary>
        /// <param name="model">Model</param>
        /// <param name="region">Region</param>
        /// <returns>XPM Markup</returns>
        HtmlString StartXpmEditingZone(string region = null);
    }

    /// <summary>
    /// Provides the View Model Key using a Component Template
    /// </summary>
    public interface IViewModelKeyProvider
    {
        /// <summary>
        /// Retrieves a View Model Key based on a Component Template. Should return the same key for the same template every time.
        /// Return values of null or empty string will be ignored.
        /// </summary>
        /// <param name="model">View Model Data to retrieve a key for</param>
        /// <returns>View Model Key</returns>
        string GetViewModelKey(IViewModelData model);
    }

    public interface ICanBeBoolean
    {
        /// <summary>
        /// True to return a boolean value
        /// </summary>
        bool IsBooleanValue { get; set; }
    }

    /// <summary>
    /// A set of methods for performing Reflection-related functions
    /// </summary>
    public interface IReflectionHelper
    {
        /// <summary>
        /// Creates an instance of an object
        /// </summary>
        /// <param name="objectType">Type of object to create</param>
        /// <returns>Object of the Type specified</returns>
        object CreateInstance(Type objectType);
        /// <summary>
        /// Creates an instance of an object
        /// </summary>
        /// <typeparam name="T">Type of object to create</typeparam>
        /// <returns>Object of the Type specified</returns>
        T CreateInstance<T>() where T : class, new();
        /// <summary>
        /// Builds a Setter delegate for the given Property
        /// </summary>
        /// <param name="propertyInfo">Property - must have a Set method</param>
        /// <returns>Setter delegate action</returns>
        Action<object, object> BuildSetter(PropertyInfo propertyInfo);
        /// <summary>
        /// Builds a Getter delegate for a given Prpoerty
        /// </summary>
        /// <param name="propertyInfo">Property</param>
        /// <returns>Getter delegate function</returns>
        Func<object, object> BuildGetter(PropertyInfo propertyInfo);
        /// <summary>
        /// Gets the PropertyInfo for a Lambda Expression
        /// </summary>
        /// <typeparam name="TSource">The source Type</typeparam>
        /// <typeparam name="TProperty">The property Type</typeparam>
        /// <param name="propertyLambda">Lambda Expression representing a Property of the source Type</param>
        /// <returns>Property Info</returns>
        PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda);
        /// <summary>
        /// Gets the PropertyInfo for a Lambda Expression
        /// </summary>
        /// <param name="source">Source object - for type inferrence of the Lambda Expression</param>
        /// <typeparam name="TSource">The source Type</typeparam>
        /// <typeparam name="TProperty">The property Type</typeparam>
        /// <param name="propertyLambda">Lambda Expression representing a Property of the source Type</param>
        /// <returns>Property Info</returns>
        PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> propertyLambda);
    }
    /// <summary>
    /// A simple representation of a Model Property
    /// </summary>
    public interface IModelProperty
    {
        /// <summary>
        /// Name of the Property
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Setter delegate
        /// </summary>
        Action<object, object> Set { get; }
        /// <summary>
        /// Getter delegate
        /// </summary>
        Func<object, object> Get { get; }
        /// <summary>
        /// The DVM4T PropertyAttribute of the Property
        /// </summary>
        IPropertyAttribute PropertyAttribute { get; }
        /// <summary>
        /// The return Type of the Property
        /// </summary>
        Type PropertyType { get; }
    }
  
    public interface IPropertyAttribute
    {
        /// <summary>
        /// The expected return type for this Property
        /// </summary>
        Type ExpectedReturnType { get; }
        /// <summary>
        /// Gets the value for this property based on a View Model object
        /// </summary>
        /// <param name="model">View Model this Property is in</param>
        /// <param name="propertyType">Actual return type of the Property</param>
        /// <param name="builder">A View Model Builder</param>
        /// <returns>Property value</returns>
        object GetPropertyValue(IViewModel model, Type propertyType, IViewModelFactory builder = null);
    }
    /// <summary>
    /// An attribute for a Property representing a Field
    /// </summary>
    public interface IFieldAttribute : IPropertyAttribute
    {
        /// <summary>
        /// Get the value of a Field
        /// </summary>
        /// <param name="field">Field for this property</param>
        /// <param name="propertyType">Actual return type of this Property</param>
        /// <param name="template">A Template for context</param>
        /// <param name="builder">A View Model builder</param>
        /// <returns>Property value</returns>
        object GetFieldValue(IFieldData field, Type propertyType, ITemplateData template, IViewModelFactory builder = null);
        /// <summary>
        /// Schema XML name of the Field
        /// </summary>
        string FieldName { get; }
        /// <summary>
        /// If true, this Field is multi-value
        /// </summary>
        bool AllowMultipleValues { get; set; }
        /// <summary>
        /// Inline Editable - for semantic purposes only
        /// </summary>
        bool InlineEditable { get; set; }
        /// <summary>
        /// Is Mandatory - for semantic purposes only
        /// </summary>
        bool Mandatory { get; set; }
        /// <summary>
        /// True if this is a Metadata Field of the Model
        /// </summary>
        bool IsMetadata { get; set; }
        /// <summary>
        /// True if this is a Metadata Field of the Template of the Model
        /// </summary>
        bool IsTemplateMetadata { get; set; }
    }

    //TODO: Anyway to merge all three interfaces into one? They're so similar
    //TODO: Use these interfaces in the builder
    
    /// <summary>
    /// An Attribute for a Property representing some part of a Component
    /// </summary>
    public interface IComponentAttribute : IPropertyAttribute
    {
        /// <summary>
        /// Gets a value for this Property based on a Component
        /// </summary>
        /// <param name="contentPresentation">The Component object for this Model</param>
        /// <param name="propertyType">The actual return type of this Property</param>
        /// 
        /// <param name="builder">A View Model builder</param>
        /// <returns>The Property value</returns>
        object GetPropertyValue(IComponentData component, Type propertyType, ITemplateData template, IViewModelFactory builder = null);
    }
    /// <summary>
    /// An Attribtue for a Property representing some part of a Component Template
    /// </summary>
    public interface ITemplateAttribute : IPropertyAttribute
    {
        /// <summary>
        /// Gets a value for this Property based on a Component Template
        /// </summary>
        /// <param name="template">The Template for this Model</param>
        /// <param name="propertyType">The actual return type of this Property</param>
        /// <param name="builder">A View Model builder</param>
        /// <returns>The Property value</returns>
        object GetPropertyValue(ITemplateData template, Type propertyType, IViewModelFactory builder = null);
    }
    /// <summary>
    /// An Attribute for a Property representing some part of a Page
    /// </summary>
    public interface IPageAttribute : IPropertyAttribute
    {
        /// <summary>
        /// Gets a value for this Property based on a Page
        /// </summary>
        /// <param name="page">The Template for this Model</param>
        /// <param name="propertyType">The actual return type of this Property</param>
        /// <param name="builder">A View Model builder</param>
        /// <returns>The Property value</returns>
        object GetPropertyValue(IPageData page, Type propertyType, IViewModelFactory builder = null);
    }
    ///// <summary>
    ///// An Attribute for a Property representing some part of a Page Template
    ///// </summary>
    //public interface IPageTemplateAttribute : IPropertyAttribute
    //{
    //    /// <summary>
    //    /// Gets a value for this Property based on a Page Template
    //    /// </summary>
    //    /// <param name="pageTemplate">The Page Template for this Model</param>
    //    /// <param name="propertyType">The actual return type of this Property</param>
    //    /// <param name="builder">A View Model builder</param>
    //    /// <returns>The Property value</returns>
    //    object GetPropertyValue(ITemplateData pageTemplate, Type propertyType, IViewModelFactory builder = null);
    //}
    /// <summary>
    /// An Attribute for identifying a View Model class
    /// </summary>
    public interface IModelAttribute
    {
        /// <summary>
        /// View Model Keys - a set of identifying values for this Model
        /// </summary>
        string[] ViewModelKeys { get; set; }
        
        
        //[Obsolete("Use IsMatch(IViewModelData, string) instead")]
        //bool IsMatch(IViewModelData data, IViewModelKeyProvider provider);

        /// <summary>
        /// Checks if this Model is a match for a specific View Model Data
        /// </summary>
        /// <param name="data">View Model Data to compare</param>
        /// <param name="key">View Model Key</param>
        /// <returns>True if it matches, false if not</returns>
        bool IsMatch(IViewModelData data, string key);
    }
    /// <summary>
    /// An Attribute for identifying a Content View Model class
    /// </summary>
    public interface IContentModelAttribute : IModelAttribute
    {
        /// <summary>
        /// XML Name of the Schema
        /// </summary>
        string SchemaName { get; }
        /// <summary>
        /// Is Inline Editable - for semantic purposes only
        /// </summary>
        bool InlineEditable { get; set; }
        /// <summary>
        /// Is this the default Model for this Schema
        /// </summary>
        bool IsDefault { get; }
    }
    /// <summary>
    /// An Attribute for identifying a Page Model class
    /// </summary>
    public interface IPageModelAttribute : IModelAttribute
    {
        //What Properties go here to identify a Page Model?
    }

    public interface IKeywordModelAttribute : IModelAttribute
    {
        //Anything?
    }
}
