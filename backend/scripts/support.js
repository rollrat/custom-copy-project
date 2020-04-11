// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// File: support.js - Preload Type/Class Definitions

/**
 * Register script to script manager.
 * This function must be called on all of the user-defined scripts.
 * @param  {string} name
 * @param  {string} author
 * @param  {string} version
 * @param  {string} id
 */
function register(name, author, version, id) {
  _register(name, author, version, id);
}

/**
 * Print info message to console
 * @param  {object} msg
 */
function info(msg) {
  if (typeof msg === 'string')
    _info(msg);
  else
    _info(JSON.stringify(msg,null,4));
}

/**
 * Print error message to console
 * @param  {object} msg
 */
function error(msg) {
  if (typeof msg === 'string')
    _error(msg);
  else
    _error(JSON.stringify(msg,null,4));
}

/**
 * Print warning message to console
 * @param  {object} msg
 */
function warning(msg) {
  if (typeof msg === 'string')
    _warning(msg);
  else
    _warning(JSON.stringify(msg,null,4));
}

/**
 * Print debug message to console
 * @param  {object} msg
 */
function debug(msg) {
  if (typeof msg === 'string')
    _debug(msg);
  else
    _debug(JSON.stringify(msg,null,4));
}

/**
 * Currently array conversion is not supported.
 * So, we run it manually.
 */
class array {

  constructor(array) {
    this.arr = array;
  }

  get length() {
    return this.arr.length;
  }

  at(index) {
    return this.arr.at(index);
  }

  map(func) {
    var result = [];
    var len = this.length;
    for (var i = 0; i < len; i++)
      result.push(func(this.arr.at(i)));
    return result;
  }

  to_array() {
    return this.map(x => x);
  }

}

/**
 * html class support
 */
class html {
  
  /**
   * Constructor of html
   * @param  {string} html
   */
  constructor(html) {
    if (typeof html === 'string')
      this.node = _native.create_html_node(html);
    else
      this.node = html;
  }
  
  /**
   * Create new html node.
   * @param  {html} html
   */
  static to_node(html) {
    return new html(html);
  }
  
  /**
   * Select single node with xpath
   * @param  {string} xpath
   * @returns {html}
   */
  select_single(xpath) {
    var node = this.node.select_single(xpath);
    if (!node) return null;
    return new html(node);
  }

  /**
   * Select nodes with xpath
   * @param  {string} xpath
   * @returns {html[]}
   */
  select(xpath) {
    var nodes = this.node.select(xpath);
    if (!nodes) return null;
    return (new array(nodes)).map(x => new html(x));
  }

  /**
   * Get attribute value of what
   * @param  {string} what
   * @returns {string}
   */
  attr(what) {
    return this.node.attr(what);
  }

  /**
   * Run cal line command.
   * @param  {string} pattern
   * @returns {html[]}
   */
  cal(pattern) {
    return (new array(this.node.cal(pattern))).map(x => new html(x));
  } 
  
  /**
   * Get inner text.
   * @returns {string}
   */
  get inner_text() {
    return this.node.inner_text;
  }

  /**
   * Get inner html
   * @returns {string}
   */
  get inner_html() {
    return this.node.inner_html;
  }

  /**
   * Get outer html
   * @returns {string}
   */
  get raw() {
    return this.node.raw;
  }
  
  /**
   * Get current node text
   * @returns {string}
   */
  get text() {
    return this.node.text;
  }

}

/**
 * download task class support
 */
class task {
  static default(url) {

  }
}

/**
 * network class support
 */
class net {

  /**
   * Download string
   * @param  {string} url
   */
  static get_string(url) {
    return _native.get_string(url);
  }

  /**
   * Download url and convert to html object
   * @param  {html} url
   */
  static get_html(url) {
    return new html(_native.get_html(url));
  }

}

/**
 * URL class support
 */
class url {
  constructor(url) {
    this.url = _native.create_url(url);
  }

  get host() {
    return this.url.host;
  }
}
