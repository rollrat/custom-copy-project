# Custom Copy Script

`Custom Copy Script` is interpreter script language for control
and extension based on `javascript`.

## How to write your own script?

```js
register("test-script", "rollrat", "1.0", "test");

info("Custom Copy Test Script V1.0");

function $url_check(url) {
  return url.host.split(".")[0] == "google";
}
function $get_card(url) {
  var card = new card();

  return card;
}
function $process(card) {
  var task = new task();

  return task;
}

debug("end of file");
```

First, you must call `register` method for attaching your script to script manager.
If you skip this step, then your script is not allowed on `Custom Copy`.

## Types and Methods

```
Static Methods
net.get_string(url: string): string
net.get_html(url: string): html
url.get_parameter(url: string, param: string): string
url.append_parameter(url: string, param: string, value: string): string
file.read(filename: string): string
file.write(filename: string, content: string): void

Dynamic Methods
html.select_single(path: string): html
html.select(path: string): html[]
html.inner_text: string
html.inner_html: string
html.raw: string
html.text: string
html.attr(what: string): string
html.cal(pattern: string): string[]
debug(msg: string): void
info(msg: string): void
error(msg: string): void
warning(msg: string): void
register(name: string, author: string, version: string, id: string): void

Dynamic Property
net.host: string
```
