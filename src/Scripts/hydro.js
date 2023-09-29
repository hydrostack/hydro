function HydroCore() {
  let promiseChain = {};

  const configMeta = document.querySelector('meta[name="hydro-config"]');
  const config = configMeta ? JSON.parse(configMeta.content) : {};

  async function loadPageContent(url, selector, push, condition, payload) {
    const element = document.querySelector(selector);
    element.classList.add('hydro-loading');

    try {
      let headers = {
        'Hydro-Request': 'true',
        'Hydro-Boosted': 'true'
      };

      if (payload) {
        headers['Hydro-Payload'] = JSON.stringify(payload);
      }

      const response = await fetch(url, {
        method: 'GET',
        headers
      });

      if (condition && !condition()) {
        throw new Error(`Request stopped`);
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      } else {
        let data = await response.text();

        let parser = new DOMParser();
        let doc = parser.parseFromString(data, 'text/html');
        let newContent = doc.querySelector(selector);
        let newTitle = doc.querySelector('head>title');
        element.innerHTML = newContent.innerHTML;

        if (newTitle) {
          document.title = newTitle.textContent;
        }

        if (push) {
          history.pushState({}, '', url);
        }
      }
    } catch (error) {
      if (error.message === 'Request stopped') {
      } else {
        console.error('Fetch error: ', error);
        window.location.href = url;
      }
    } finally {
      element.classList.remove('hydro-loading');
    }
  }

  function enqueueHydroPromise(key, promiseFunc) {
    if (!promiseChain[key]) {
      promiseChain[key] = Promise.resolve();
    }

    let lastPromise = promiseChain[key];
    promiseChain[key] = promiseChain[key].then(() =>
      promiseFunc()
        .catch(error => {
          console.log(`Error: ${error}`);
        })
    );
    return lastPromise;
  }

  function findComponent(el) {
    const component = el.closest("[hydro]");

    if (!component) {
      return null;
    }

    return {
      id: component.getAttribute("id"),
      name: component.getAttribute("hydro-name"),
      element: component
    };
  }

  let binding = {};

  async function hydroEvent(el, url, requestModel) {
    const body = JSON.stringify(requestModel);
    const hydroEvent = el.getAttribute("x-on-hydro-event");
    const wireEventData = JSON.parse(hydroEvent);
    await hydroRequest(el, url, 'application/json', body, null, wireEventData);
  }

  async function hydroBind(el) {
    if (!el.hasAttribute('x-hydro-bind')) {
      return;
    }

    const component = findComponent(el);

    if (!component) {
      throw new Error('Cannot find Hydro component');
    }

    const url = `/hydro/${component.name}`;

    if (!binding[url]) {
      binding[url] = {
        formData: new FormData()
      };
    }

    if (binding[url].timeout) {
      clearTimeout(binding[url].timeout);
    }

    binding[url].formData.set(el.getAttribute('name'), el.value);

    return await new Promise(resolve => {
      binding[url].timeout = setTimeout(async () => {
        await hydroRequest(el, url, null, binding[url].formData, 'bind');
        binding[url].formData = new FormData();
        resolve();
      }, 10);
    });
  }

  async function hydroAction(el, eventName) {
    const url = el.getAttribute('x-hydro-action');

    if (document.activeElement && document.activeElement.hasAttribute('x-hydro-bind') && isElementDirty(document.activeElement)) {
      await hydroBind(document.activeElement);
    }

    const formData = eventName === 'submit'
      ? new FormData(el.closest("form"))
      : null;

    await hydroRequest(el, url, null, formData);
  }

  async function hydroRequest(el, url, contentType, body, type, eventData) {
    const component = el.closest("[hydro]");
    const componentId = component.getAttribute("id");

    if (!component) {
      throw new Erorr('Cannot determine the closest Hydro component');
    }

    const parentComponent = findComponent(component.parentElement);

    const classTimeout = setTimeout(() => el.classList.add('hydro-request'), 200);

    let elementsToDisable = [];

    let disableTimer = null;

    if (!eventData && type !== 'bind') {
      elementsToDisable = [el];

      disableTimer = setTimeout(() => {
        elementsToDisable.forEach((elementToDisable) => {
          elementToDisable.disabled = true;
        });
      }, 200);
    }

    await enqueueHydroPromise('', async () => {
      try {
        let headers = {
          'Hydro-Request': 'true'
        };

        if (contentType) {
          headers['Content-Type'] = contentType;
        }

        if (config.Antiforgery) {
          headers[config.Antiforgery.HeaderName] = config.Antiforgery.Token;
        }

        const scripts = component.querySelectorAll('script[data-id]');
        const dataIds = Array.from(scripts).map(script => script.getAttribute('data-id')).filter(d => d !== componentId);
        headers['hydro-all-ids'] = JSON.stringify([componentId, ...dataIds]);

        const scriptTag = component.querySelector(`script[data-id='${componentId}']`);
        headers['hydro-model'] = scriptTag.textContent;

        if (eventData) {
          headers['hydro-event-name'] = eventData.name;
        }

        const parameters = el.getAttribute("hydro-parameters");

        if (parameters) {
          headers['Hydro-Parameters'] = parameters;
        }

        const response = await fetch(url, {
          method: 'POST',
          body: body,
          headers: headers
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        } else {
          const responseData = await response.text();
          let counter = 0;

          Alpine.morph(component, responseData, {
            updating: (from, to, childrenOnly, skip) => {
              if (counter !== 0 && to.getAttribute && to.getAttribute("hydro") !== null && from.getAttribute && from.getAttribute("hydro") !== null) {
                skip();
              }

              if (from.tagName === "INPUT" && from.type === 'checkbox') {
                from.checked = to.checked;
              }

              if (from.tagName === "INPUT" && from.type === 'text' && from.value !== to.getAttribute("value")) {
                to.setAttribute("data-update-on-blur", "");
              }

              counter++;
            }
          });

          const locationHeader = response.headers.get('Hydro-Location');
          if (locationHeader) {
            let locationData = JSON.parse(locationHeader);

            await loadPageContent(locationData.path, locationData.target || 'body', true, null, locationData.payload);
          }

          const redirectHeader = response.headers.get('Hydro-Redirect');
          if (redirectHeader) {
            window.location.href = redirectHeader;
          }

          const triggerHeader = response.headers.get('Hydro-Trigger');
          if (triggerHeader) {
            const triggers = JSON.parse(triggerHeader);
            triggers.forEach(trigger => {
              if (trigger.scope === 'parent' && !parentComponent) {
                return;
              }

              const scope = trigger.scope === 'parent' ? parentComponent.id : 'global';
              const event = new CustomEvent(`${scope}:${trigger.name}`, {detail: trigger.data});

              setTimeout(() => {
                document.dispatchEvent(event);
              }, 0)
            });
          }
        }
      } finally {
        if (!eventData) {
          clearTimeout(classTimeout);
          clearTimeout(disableTimer);
          el.classList.remove('hydro-request');

          elementsToDisable.forEach((elementToDisable) => {
            elementToDisable.disabled = false;
          });
        }
      }
    });
  }

  function isElementDirty(element) {
    const type = element.type;
    if (type === "checkbox" || type === "radio") {
      if (element.checked !== element.defaultChecked) {
        return true;
      }
    } else if (type === "hidden" || type === "password" || type === "text" || type === "textarea") {
      if (element.value !== element.defaultValue) {
        return true;
      }
    } else if (type === "select-one" || type === "select-multiple") {
      for (let j = 0; j < element.options.length; j++) {
        if (element.options[j].selected !== element.options[j].defaultSelected) {
          return true;
        }
      }
    }
  }

  window.addEventListener('popstate', async function () {
    await loadPageContent(window.location.href, 'body', false);
  });

  return {
    hydroEvent,
    hydroBind,
    hydroAction,
    loadPageContent,
    findComponent,
    config
  };
}

window.Hydro = new HydroCore();

document.addEventListener('alpine:init', () => {
  Alpine.directive('hydro-action', (el, {expression}, {effect, cleanup}) => {
    effect(() => {
      const customEvent = el.getAttribute('hydro-event');
      const eventName = customEvent || (el.tagName === 'FORM' ? 'submit' : 'click');
      const delay = el.getAttribute('hydro-delay');
      const autorun = el.getAttribute('hydro-autorun');

      if (autorun) {
        setTimeout(() => window.Hydro.hydroAction(el), delay || 0)
      } else {
        const eventHandler = async (event) => {
          event.preventDefault();
          setTimeout(() => window.Hydro.hydroAction(el, eventName), delay || 0)
        };
        el.addEventListener(eventName, eventHandler);
        cleanup(() => {
          el.removeEventListener(eventName, eventHandler);
        });
      }
    });
  });

  Alpine.directive('hydro-bind', (el, {expression}, {effect, cleanup}) => {
    effect(() => {
      const event = expression || "change";

      const eventHandler = async (event) => {
        event.preventDefault();
        await window.Hydro.hydroBind(event.target);
      };

      const blurHandler = async (event) => {
        if (event.target.getAttribute("data-update-on-blur") !== null) {
          await window.Hydro.hydroBind(event.target);
          event.target.value = event.target.getAttribute("value");
        }
      };

      el.addEventListener(event, eventHandler);
      el.addEventListener("blur", blurHandler);
      cleanup(() => {
        el.removeEventListener(event, eventHandler);
        el.removeEventListener("blur", blurHandler);
      });
    });
  });

  Alpine.directive('on-hydro-event', (el, {expression}, {effect, cleanup}) => {
    effect(() => {
      const component = window.Hydro.findComponent(el);

      if (!component) {
        throw new Error("Cannot find Hydro component");
      }

      const data = JSON.parse(expression);
      const globalEventName = `global:${data.name}`;
      const localEventName = `${component.id}:${data.name}`;
      const eventPath = data.path;

      const eventHandler = async (event) => {
        await window.Hydro.hydroEvent(el, eventPath, event.detail);
      }

      document.addEventListener(globalEventName, eventHandler);
      document.addEventListener(localEventName, eventHandler);

      cleanup(() => {
        document.removeEventListener(globalEventName, eventHandler);
        document.removeEventListener(localEventName, eventHandler);
      });
    });
  });


  let currentBoostUrl;

  Alpine.directive('boost', (el, {expression}, {effect, cleanup}) => {
    effect(() => {
      const handleClick = async (event) => {
        event.preventDefault();
        const el = event.target;
        const url = el.getAttribute('href');
        currentBoostUrl = url;
        const classTimeout = setTimeout(() => el.classList.add('hydro-request'), 200);
        try {
          await window.Hydro.loadPageContent(url, 'body', true, () => currentBoostUrl === url);
        } finally {
          clearTimeout(classTimeout);
          el.classList.remove('hydro-request')
        }
      };

      const links = [el, ...el.querySelectorAll('a')].filter(el => el.tagName === 'A');
      links.forEach((link) => {
        link.addEventListener('click', handleClick);
      });

      cleanup(() => {
        links.forEach((link) => {
          link.removeEventListener('click', handleClick);
        });
      });
    });
  });
});
