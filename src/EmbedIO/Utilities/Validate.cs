﻿using System;
using System.IO;
using System.Security;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides validation methods for method arguments.
    /// </summary>
    public static class Validate
    {
        private static readonly char[] InvalidLocalPathChars = GetInvalidLocalPathChars();

        /// <summary>
        /// Ensures that an argument is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The type of the argument to validate.</typeparam>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/> if not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static T NotNull<T>(string argumentName, T value)
            where T : class
            => value ?? throw new ArgumentNullException(argumentName);

        /// <summary>
        /// Ensures that a nullable argument is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The base type of the argument to validate.</typeparam>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/> if not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static T? NotNull<T>(string argumentName, T? value)
            where T : struct
            => value ?? throw new ArgumentNullException(argumentName);

        /// <summary>
        /// Ensures that a <see langword="string"/> argument is neither <see langword="null"/> nor the empty string.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/> if neither <see langword="null"/> nor the empty string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is the empty string.</exception>
        public static string NotNullOrEmpty(string argumentName, string value)
        {
            if (value == null)
                throw new ArgumentNullException(argumentName);

            if (value.Length == 0)
                throw new ArgumentException("String is empty.", argumentName);

            return value;
        }

        /// <summary>
        /// Ensures that the value of an argument is a valid enumeration constant.
        /// </summary>
        /// <typeparam name="T">The type of the argument to validate.</typeparam>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/> if valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is not valid.</exception>
        /// <remarks>
        /// <para>Do not use this method to validate arguments whose type
        /// has the <see cref="FlagsAttribute">Flags</see> attribute.</para>
        /// </remarks>
        public static T EnumValue<T>(string argumentName, T value)
            where T : Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException("Value is not valid.", argumentName);

            return value;
        }

        /// <summary>
        /// Ensures that the value of an argument is a valid URL path
        /// and normalizes it.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="isBasePath">If set to <see langword="true"/><c>true</c> [is base path].</param>
        /// <returns>The normalized URL path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> does not start with a slash (<c>/</c>) character.</para>
        /// </exception>
        public static string UrlPath(string argumentName, string value, bool isBasePath)
        {
            var exception = Utilities.UrlPath.ValidateInternal(argumentName, value);
            if (exception != null)
                throw exception;

            return Utilities.UrlPath.Normalize(value, isBasePath);
        }

        /// <summary>
        /// Ensures that the value of an argument is a valid local path
        /// and, optionally, gets the corresponding full path.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="getFullPath"><see langword="true"/> to get the full path, <see langword="false"/> to leave the path as is..</param>
        /// <returns>The local path, or the full path if <paramref name="getFullPath"/> is <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains only white space.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more invalid characters.</para>
        /// <para>- or -</para>
        /// <para><paramref name="getFullPath"/> is <see langword="true"/> and the full path could not be obtained.</para>
        /// </exception>
        public static string LocalPath(string argumentName, string value, bool getFullPath)
        {
            if (value == null)
                throw new ArgumentNullException(argumentName);

            if (value.Length == 0)
                throw new ArgumentException("Local path is empty.", argumentName);

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Local path contains only white space.", argumentName);

            if (value.IndexOfAny(InvalidLocalPathChars) >= 0)
                throw new ArgumentException("Local path contains one or more invalid characters.", argumentName);

            if (getFullPath)
            {
                try
                {
                    value = Path.GetFullPath(value);
                }
                catch (Exception e) when (e is ArgumentException || e is SecurityException || e is NotSupportedException || e is PathTooLongException)
                {
                    throw new ArgumentException("Could not get the full local path.", argumentName, e);
                }
            }

            return value;
        }

        /// <summary>
        /// Ensures that a valid URL can be constructed from a <see langword="string"/> argument.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="value">The value.</param>
        /// <param name="uriKind">Specifies whether <paramref name="value"/> is a relative URL, absolute URL, or is indeterminate.</param>
        /// <param name="enforceHttp">Ensure that, if <paramref name="value"/> is an absolute URL, its scheme is either <c>http</c> or <c>https</c>.</param>
        /// <returns>The string representation of the constructed URL.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is not a valid URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="enforceHttp"/> is <see langword="true"/>, <paramref name="value"/> is an absolute URL,
        /// and <paramref name="value"/>'s scheme is neither <c>http</c> nor <c>https</c>.</para>
        /// </exception>
        /// <seealso cref="Url(string,string,Uri,bool)"/>
        public static string Url(string argumentName, string value, UriKind uriKind = UriKind.RelativeOrAbsolute, bool enforceHttp = false)
        {
            Uri uri;
            try
            {
                uri = new Uri(NotNull(argumentName, value), uriKind);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException("URL is not valid.", argumentName, e);
            }

            if (enforceHttp && uri.IsAbsoluteUri && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("URL scheme is neither HTTP nor HTTPS.", argumentName);

            return uri.ToString();
        }

        /// <summary>
        /// Ensures that a valid URL, either absolute or relative to the given <paramref name="baseUri"/>,
        /// can be constructed from a <see langword="string"/> argument and returns the absolute URL
        /// obtained by combining <paramref name="baseUri"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="value">The value.</param>
        /// <param name="baseUri">The base URI for relative URLs.</param>
        /// <param name="enforceHttp">Ensure that the resulting URL's scheme is either <c>http</c> or <c>https</c>.</param>
        /// <returns>The string representation of the constructed URL.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="baseUri"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="baseUri"/> is not an absolute URI.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> is not a valid URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="enforceHttp"/> is <see langword="true"/>, 
        /// and the combination of <paramref name="baseUri"/> and <paramref name="value"/> has a scheme
        /// that is neither <c>http</c> nor <c>https</c>.</para>
        /// </exception>
        /// <seealso cref="Url(string,string,UriKind,bool)"/>
        public static string Url(string argumentName, string value, Uri baseUri, bool enforceHttp = false)
        {
            if (!NotNull(nameof(baseUri), baseUri).IsAbsoluteUri)
                throw new ArgumentException("Base URI is not an absolute URI.", nameof(baseUri));

            Uri uri;
            try
            {
                uri = new Uri(baseUri,new Uri(NotNull(argumentName, value), UriKind.RelativeOrAbsolute));
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException("URL is not valid.", argumentName, e);
            }

            if (enforceHttp && uri.IsAbsoluteUri && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("URL scheme is neither HTTP nor HTTPS.", argumentName);

            return uri.ToString();
        }

        private static char[] GetInvalidLocalPathChars()
        {
            var systemChars = Path.GetInvalidPathChars();
            var p = systemChars.Length;
            var result = new char[p + 2];
            Array.Copy(systemChars, result, p);
            result[p++] = '*';
            result[p] = '?';
            return result;
        }
    }
}