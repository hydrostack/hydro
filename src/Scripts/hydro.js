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
        const eventDetail = {
          message: "Problem with loading the content",
          data: null
        }
        document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: eventDetail } }));
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
          // throw error;
        })
    );
    return promiseChain[key];
  }

  function generateGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
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
  let dirty = {};

  async function hydroEvent(el, url, eventData) {
    const operationId = eventData.operationId;
    const body = JSON.stringify(eventData.data);
    const hydroEvent = el.getAttribute("x-on-hydro-event");
    const wireEventData = JSON.parse(hydroEvent);
    await hydroRequest(el, url, 'application/json', body, 'event', wireEventData, operationId);
  }

  async function hydroBind(el) {
    if (!isElementDirty(el)) {
      return;
    }

    const component = findComponent(el);

    if (!component) {
      throw new Error('Cannot find Hydro component');
    }

    const url = `/hydro/${component.name}`;

    if (!binding[component.id]) {
      binding[component.id] = {
        formData: new FormData()
      };
    }

    let propertyName = el.getAttribute('name');

    const value = el.tagName === "INPUT" && el.type === 'checkbox' ? el.checked : el.value;
    const bindAlreadyInitialized = [...binding[component.id].formData].length !== 0;

    binding[component.id].formData.set(propertyName, value);

    if (bindAlreadyInitialized) {
      return Promise.resolve();
    }

    if (binding[component.id].timeout) {
      clearTimeout(binding[component.id].timeout);
    }

    return await new Promise(resolve => {
      binding[component.id].timeout = setTimeout(async () => {
        const requestFormData = binding[component.id].formData;
        // binding[url].formData = new FormData();
        const bindOperationId = generateGuid();
        dirty[propertyName] = bindOperationId;
        await hydroRequest(el, url, null, requestFormData, 'bind', null, bindOperationId);
        if (dirty[propertyName] === bindOperationId) {
          delete dirty[propertyName];
        }
        resolve();
      }, 10);
    });
  }

  async function hydroAction(el, component, action, clientEvent) {
    const url = `/hydro/${component.name}/${action.name}`;

    if (Array.from(el.attributes).some(attr => attr.name.startsWith('x-hydro-bind')) && isElementDirty(el)) {
      await hydroBind(el);
    }

    if (document.activeElement && document.activeElement !== el) {
      const elToCheck = document.activeElement;
      if (Array.from(elToCheck.attributes).some(attr => attr.name.startsWith('x-hydro-bind')) && isElementDirty(elToCheck)) {
        await hydroBind(elToCheck);
      }
    }

    const operationId = generateGuid();
    el.setAttribute("hydro-operation-id", operationId);

    await hydroRequest(el, url, null, null, null, null, operationId, true, JSON.stringify(action.parameters || {}), clientEvent);
  }

  let operationStatus = {};

  async function hydroRequest(el, url, contentType, body, type, eventData, operationId, morphActiveElement, params, clientEvent) {
    if (!document.contains(el)) {
      return;
    }

    const component = el.closest("[hydro]");
    const componentId = component.getAttribute("id");

    if (!component) {
      throw new Erorr('Cannot determine the closest Hydro component');
    }

    const parentComponent = findComponent(component.parentElement);

    let disableTimer;
    let classTimeout;

    if (operationId) {
      if (!operationStatus[operationId]) {
        operationStatus[operationId] = 0;

        const operationTrigger = document.querySelector(`[hydro-operation-id="${operationId}"]`);

        if (operationTrigger) {
          classTimeout = setTimeout(() => operationTrigger.classList.add('hydro-request'), 200);
          disableTimer = setTimeout(() => operationTrigger.disabled = true, 200);
        }
      }

      operationStatus[operationId]++;
    }

    await enqueueHydroPromise(componentId, async () => {
      try {
        let headers = {
          'Hydro-Request': 'true'
        };

        if (contentType) {
          headers['Content-Type'] = contentType;
        }

        if (clientEvent) {
          headers['Hydro-Client-Event-Name'] = clientEvent.type;
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

        const parameters = params || el.getAttribute("hydro-parameters");

        if (parameters) {
          headers['Hydro-Parameters'] = parameters;
        }

        if (operationId) {
          headers['Hydro-Operation-Id'] = operationId;
        }

        if (type === 'bind') {
          binding[componentId].formData = new FormData();
        }

        const response = await fetch(url, {
          method: 'POST',
          body: body,
          headers: headers
        });

        if (!response.ok) {
          if (response.status === 400 && config.Antiforgery && response.headers.get("Refresh-Antiforgery-Token")) {
            const json = await response.json();
            config.Antiforgery.Token = json.token;
          } else if (response.status === 403) {
            if (type !== 'event') {
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: { message: "Unauthorized access" } } }));
            }
            throw new Error(`HTTP error! status: ${response.status}`);
          } else {
            const contentType = response.headers.get("content-type");
            let eventDetail = {};

            try {
              if (contentType && contentType.indexOf("application/json") !== -1) {
                const json = await response.json();
                eventDetail.message = json?.message || "Unhandled exception";
                eventDetail.data = json?.data;
              } else {
                eventDetail.message = await response.text();
              }
            } catch {
              // ignore
            }

            if (type !== 'event') {
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: eventDetail } }));
            }
            throw new Error(`HTTP error! status: ${response.status}`);
          }
        } else {
          const skipOutputHeader = response.headers.get('Hydro-Skip-Output');

          if (!skipOutputHeader) {
            const responseData = await response.text();
            let counter = 0;

            Alpine.morph(component, responseData, {
              updating: (from, to, childrenOnly, skip) => {
                if (counter !== 0 && to.getAttribute && to.getAttribute("hydro") !== null && from.getAttribute && from.getAttribute("hydro") !== null) {
                  if (to.getAttribute("hydro-placeholder") !== null) {
                    skip();
                  }
                }

                if (from.getAttribute && from.getAttribute("hydro-operation-id")) {
                  to.setAttribute("hydro-operation-id", from.getAttribute("hydro-operation-id"));
                  to.disabled = from.disabled;
                  if (from.classList.contains('hydro-request')) {
                    to.classList.add('hydro-request');
                  }
                }

                const fieldName = from.getAttribute && from.getAttribute("name");

                if (fieldName && dirty[fieldName] && dirty[fieldName] !== operationId) {
                  skip();
                } else {
                  if (from.tagName === "INPUT" && from.type === 'checkbox') {
                    from.checked = to.checked;
                  }

                  if (from.tagName === "INPUT" && ['text', 'number', 'date'].includes(from.type) && from.value !== to.getAttribute("value")) {
                    if (document.activeElement !== from || (morphActiveElement && from.value !== to.value)) {
                      from.value = to.value;
                    }
                  }
                }

                counter++;
              }
            });
          }

          const locationHeader = response.headers.get('Hydro-Location');
          if (locationHeader) {
            let locationData = JSON.parse(locationHeader);

            await loadPageContent(locationData.path, locationData.target || 'body', true, null, locationData.payload);
          }

          const redirectHeader = response.headers.get('Hydro-Redirect');
          if (redirectHeader) {
            window.location.href = redirectHeader;
          }

          setTimeout(() => { // make sure the event handlers got registered
            const triggerHeader = response.headers.get('Hydro-Trigger');
            if (triggerHeader) {
              const triggers = JSON.parse(triggerHeader);
              triggers.forEach(trigger => {
                if (trigger.scope === 'parent' && !parentComponent) {
                  return;
                }

                const eventScope = trigger.scope === 'parent' ? parentComponent.id : 'global';
                const eventName = `${eventScope}:${trigger.name}`;
                const eventData = {
                  detail: {
                    data: trigger.data,
                    operationId: trigger.operationId
                  }
                };

                document.dispatchEvent(new CustomEvent(eventName, eventData));
              });
            }
          });
        }
      } finally {
        setTimeout(() => {
          clearTimeout(classTimeout);
          clearTimeout(disableTimer);

          if (operationId) {
            const operationTrigger = document.querySelector(`[hydro-operation-id="${operationId}"]`);
            operationStatus[operationId]--;

            if (operationTrigger && operationStatus[operationId] <= 0) {
              operationTrigger.disabled = false;
              operationTrigger.classList.remove('hydro-request');
            }
          }
        }, 20); // make sure it's delayed more than 0 and less than 50
      }
    });
  }

  function isElementDirty(element) {
    const type = element.type;
    if (['checkbox', 'radio'].includes(type)) {
      if (element.checked !== element.defaultChecked) {
        return true;
      }
    } else if (['hidden', 'password', 'text', 'textarea', 'number'].includes(type)) {
      if (element.value !== element.defaultValue) {
        return true;
      }
    } else if (["select-one", "select-multiple"].includes(type)) {
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
    generateGuid,
    hydroRequest,
    config
  };
}

window.Hydro = new HydroCore();

document.addEventListener('alpine:init', () => {
  Alpine.directive('hydro-dispatch', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      if (!document.contains(el)) {
        return;
      }

      const component = window.Hydro.findComponent(el);

      if (!component) {
        throw new Error("Cannot find Hydro component");
      }

      const eventName = el.getAttribute('hydro-event') || 'click';

      if (!component.element.parentElement) {
        debugger;
      }

      const parentComponent = window.Hydro.findComponent(component.element.parentElement);

      const trigger = JSON.parse(expression);

      if (trigger.scope === 'parent' && !parentComponent) {
        return;
      }

      const scope = trigger.scope === 'parent' ? parentComponent.id : 'global';

      const eventHandler = async (event) => {
        if (el.disabled) {
          return;
        }

        event.preventDefault();

        const operationId = window.Hydro.generateGuid();
        el.setAttribute("hydro-operation-id", operationId);

        const eventName = `${scope}:${trigger.name}`;
        const eventData = {
          detail: {
            data: trigger.data,
            operationId
          }
        };

        document.dispatchEvent(new CustomEvent(eventName, eventData));
      };
      el.addEventListener(eventName, eventHandler);
      cleanup(() => {
        el.removeEventListener(eventName, eventHandler);
      });
    });
  });

  Alpine.directive('hydro-bind', (el, { value, modifiers }, { effect, cleanup }) => {
    effect(() => {
      const event = value;

      const debounce = parseInt(((modifiers[0] === 'debounce' && (modifiers[1] || '250ms')) || '0ms').replace('ms', ''));

      let timeout = 0;

      const eventHandler = async (event) => {
        if (event === 'submit' || event === 'click') {
          event.preventDefault();
        }

        clearTimeout(timeout);

        const target = event.currentTarget;
        timeout = setTimeout(async () => {
          await window.Hydro.hydroBind(target);
        }, debounce);
      };

      el.addEventListener(event, eventHandler);
      cleanup(() => {
        el.removeEventListener(event, eventHandler);
      });
    });
  }).before('on');
  
  Alpine.directive('hydro-polling', Alpine.skipDuringClone((el, { value, expression, modifiers }, { effect, cleanup }) => {
    let isQueued = false;
    let interval;
    const component = window.Hydro.findComponent(el);
    const time = parseInt(modifiers[0].replace('ms', ''));

    const setupInterval = () => {
      interval = setInterval(async () => {
        if (document.hidden) {
          isQueued = true;
          clearInterval(interval);
          return;
        }

        await window.Hydro.hydroAction(el, component, { name: expression }, null);
      }, time);
    }

    const handleVisibilityChange = async () => {
      if (!document.hidden && isQueued) {
        isQueued = false;
        await window.Hydro.hydroAction(el, component, { name: expression });
        setupInterval();
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange);
    setupInterval();

    cleanup(() => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      clearInterval(interval);
    });

  }));

  Alpine.directive('on-hydro-event', (el, { expression }, { effect, cleanup }) => {
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

  Alpine.directive('hydro-link', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      const handleClick = async (event) => {
        event.preventDefault();
        const link = event.currentTarget;
        const url = link.getAttribute('href');
        currentBoostUrl = url;
        const classTimeout = setTimeout(() => link.classList.add('hydro-request'), 200);
        try {
          await window.Hydro.loadPageContent(url, 'body', true, () => currentBoostUrl === url);
        } finally {
          clearTimeout(classTimeout);
          link.classList.remove('hydro-request')
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

  Alpine.directive('hydro-focus', (el, { expression }, { effect, cleanup }) => {
    effect(() => {
      el.querySelector(expression || 'input').focus();
    });
  });

  Alpine.data('hydro', () => {
    let debounceArray = {};

    return ({
      $component: null,
      init() {
        this.$component = window.Hydro.findComponent(this.$el);
      },
      async invoke(e, action) {
        if (["click", "submit"].includes(e.type)) {
          e.preventDefault();
        }
        await window.Hydro.hydroAction(this.$el, this.$component, action, e);
      },
      async bind(debounce) {
        let element = this.$el;

        clearTimeout(debounceArray[element]);

        debounceArray[element] = setTimeout(async () => {
          await window.Hydro.hydroBind(element);
          delete debounceArray[element];
        }, debounce || 0);
      }
    });
  })
});
