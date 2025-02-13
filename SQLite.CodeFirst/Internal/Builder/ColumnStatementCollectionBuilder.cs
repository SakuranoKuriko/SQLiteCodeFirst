﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using SQLite.CodeFirst.Extensions;
using SQLite.CodeFirst.Statement;
using SQLite.CodeFirst.Statement.ColumnConstraint;

namespace SQLite.CodeFirst.Builder
{
    internal class ColumnStatementCollectionBuilder : IStatementBuilder<ColumnStatementCollection>
    {
        private readonly IEnumerable<EdmProperty> properties;
        private readonly IEnumerable<EdmProperty> keyMembers;
        private readonly Collation defaultCollation;

        public ColumnStatementCollectionBuilder(IEnumerable<EdmProperty> properties, IEnumerable<EdmProperty> keyMembers, Collation defaultCollation)
        {
            this.properties = properties;
            this.keyMembers = keyMembers;
            this.defaultCollation = defaultCollation;
        }

        public ColumnStatementCollection BuildStatement()
        {
            var columnDefStatement = new ColumnStatementCollection(CreateColumnStatements().ToList());
            return columnDefStatement;
        }

        private IEnumerable<ColumnStatement> CreateColumnStatements()
        {
            foreach (var property in properties)
            {
                var columnStatement = new ColumnStatement
                {
                    ColumnName = property.Name,
                    TypeName = property.TypeName,
                    ColumnConstraints = new ColumnConstraintCollection()
                };

                AddMaxLengthConstraintIfNecessary(property, columnStatement);
                AdjustDatatypeForAutogenerationIfNecessary(property, columnStatement);
                AddNullConstraintIfNecessary(property, columnStatement);
                AddUniqueConstraintIfNecessary(property, columnStatement);
                AddCaseSensitiveConstraintIfNecessary(property, columnStatement);
                AddCollationConstraintIfNecessary(property, columnStatement, defaultCollation);
                AddPrimaryKeyConstraintAndAdjustTypeIfNecessary(property, columnStatement);
                AddDefaultValueConstraintIfNecessary(property, columnStatement);

                yield return columnStatement;
            }
        }

        private static void AddMaxLengthConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            if (property.MaxLength.HasValue)
            {
                columnStatement.ColumnConstraints.Add(new MaxLengthConstraint(property.MaxLength.Value));
            }
        }

        private static void AdjustDatatypeForAutogenerationIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            if (property.StoreGeneratedPattern == StoreGeneratedPattern.Identity)
            {
                // Must be INTEGER else SQLite will not generate the Ids
                ConvertIntegerType(columnStatement);
            }
        }

        private static void AddNullConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            if (!property.Nullable && property.StoreGeneratedPattern != StoreGeneratedPattern.Identity)
            {
                // Only mark it as NotNull if it should not be generated.
                columnStatement.ColumnConstraints.Add(new NotNullConstraint());
            }
        }

        private static void AddCollationConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement, Collation defaultCollation)
        {
            var collateAttribute = property.GetCustomAnnotation<CollateAttribute>();
            if (property.PrimitiveType.PrimitiveTypeKind == PrimitiveTypeKind.String)
            {
                // The column is a string type. Check if we have an explicit or default collation.
                // If we have both, the explicitly chosen collation takes precedence.
                var value = collateAttribute == null ? defaultCollation : collateAttribute.Collation;
                if (value != null)
                {
                    columnStatement.ColumnConstraints.Add(new CollateConstraint { CollationFunction = value.Function, CustomCollationFunction = value.CustomFunction });
                }
            }
            else if (collateAttribute != null)
            {
                // Only string columns can be explicitly decorated with CollateAttribute.
                var name = $"{property.DeclaringType.Name}.{property.Name}";
                var errorMessage = $"CollateAttribute cannot be used on non-string property: {name} (underlying type is {property.PrimitiveType.PrimitiveTypeKind})";
                throw new InvalidOperationException(errorMessage);
            }
        }

        private static void AddUniqueConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            var value = property.GetCustomAnnotation<UniqueAttribute>();
            if (value != null)
            {
                columnStatement.ColumnConstraints.Add(new UniqueConstraint { OnConflict = value.OnConflict });
            }
        }

        private static void AddDefaultValueConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            var value = property.GetCustomAnnotation<SqlDefaultValueAttribute>();
            if (value != null)
            {
                columnStatement.ColumnConstraints.Add(new DefaultValueConstraint { DefaultValue = value.DefaultValue });
            }
        }

        private void AddPrimaryKeyConstraintAndAdjustTypeIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            // Only handle a single primary key this way.
            if (keyMembers.Count() != 1 || !property.Equals(keyMembers.Single()))
            {
                return;
            }

            ConvertIntegerType(columnStatement);
            var primaryKeyConstraint = new PrimaryKeyConstraint();
            primaryKeyConstraint.Autoincrement = property.GetCustomAnnotation<AutoincrementAttribute>() != null;
            columnStatement.ColumnConstraints.Add(primaryKeyConstraint);
        }

        private static void AddCaseSensitiveConstraintIfNecessary(EdmProperty property, ColumnStatement columnStatement)
        {
            var attribute = property.GetCustomAnnotation<CaseSensitiveAttribute>();
            if (attribute != null)
            {
                columnStatement.ColumnConstraints.Add(new CaseSensitiveConstraint { IsCaseSensitive = attribute.IsCaseSensitive });
            }
        }

        private static void ConvertIntegerType(ColumnStatement columnStatement)
        {
            const string integerType = "INTEGER";
            columnStatement.TypeName = columnStatement.TypeName.ToUpperInvariant() == "INT" ? integerType : columnStatement.TypeName;
        }
    }
}
