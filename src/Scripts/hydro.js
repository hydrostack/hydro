function HydroCore() {
  let promiseChain = {};

  const configMeta = document.querySelector('meta[name="hydro-config"]');
  const config = configMeta ? JSON.parse(configMeta.content) : {};
  let currentPathname = document.location.pathname + document.location.search;

  function createScriptTag(content, src, autoRemove) {
    const script = document.createElement('script');
    script.type = 'text/javascript';
    script.innerHTML = content + ";";

    if (autoRemove) {
      script.id = generateGuid();
      script.innerHTML += `setTimeout(() => document.getElementById("${script.id}").remove());`;
    }

    script.async = true;
    if (src) {
      script.src = src;
    }
    script.setAttribute('hydro-loaded', 'true');
    return script;
  }

  function enableScripts(element, selector) {
    const scripts = Array.from(element.querySelectorAll(selector));

    for (const script of scripts) {
      const newScript = createScriptTag(script.innerHTML, script.src);
      script.parentNode.replaceChild(newScript, script);
    }
  }

  function appendComponentScripts(component, scripts) {
    scripts.forEach(script => {
      const scriptTag = createScriptTag(`Hydro.executeComponentJs('${component.id}', function() { ${script} })`, null, true);
      document.body.appendChild(scriptTag);
    });
  }

  function enablePlainScripts(element) {
    enableScripts(element, 'script[type="text/javascript"]:not([hydro-loaded]):not([hydro-js])');
  }

  function enableHydroScripts(componentElement) {
    enableScripts(componentElement, `script[hydro-js][hydro-id="${componentElement.id}"]:not([hydro-loaded])`);
  }

  function executeComponentJs(componentId, func) {
    const callback = () => func.call(document.getElementById(componentId))

    if (document.readyState === "complete") {
      setTimeout(callback);
    } else {
      document.addEventListener("DOMContentLoaded", callback);
    }
  }

  async function loadPageContent(url, push, condition, payload) {
    document.body.classList.add('hydro-loading');

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
        document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json(eventDetail) } }));
        throw new Error(`HTTP error! status: ${response.status}`);
      } else {
        const data = await response.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(data, 'text/html');
        const selector = response.headers.get('Hydro-Location-Target') || 'body'
        const newContent = doc.querySelector(selector);
        const html = newContent ? newContent.innerHTML : data;

        let newTitle = response.headers.get('Hydro-Location-Title') || doc.querySelector('head>title')?.textContent;
        const element = document.querySelector(selector);
        element.innerHTML = html;

        if (newTitle) {
          document.title = newTitle;
        }

        if (push) {
          history.pushState({}, '', url);
        }

        if (window.location.hash) {
          document.querySelector(window.location.hash).scrollIntoView();
        } else {
          window.scrollTo(0, 0);
        }

        currentPathname = document.location.pathname + document.location.search;

        enablePlainScripts(element);

        document.dispatchEvent(new CustomEvent('HydroLocation', {
          detail: { url, selector, push, payload }
        }));
      }
    } catch (error) {
      if (error.message === 'Request stopped') {
      } else {
        console.error('Fetch error: ', error);
        window.location.href = url;
      }
    } finally {
      document.body.classList.remove('hydro-loading');
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

  async function hydroEvent(el, hydroEventRaw, url, eventData) {
    const operationId = eventData.operationId;
    const hydroEvent = JSON.parse(hydroEventRaw);
    el.setAttribute("data-operation-id", operationId);

    await hydroRequest(el, url, { eventData: { name: hydroEvent.name, data: eventData.data, subject: eventData.subject } }, 'event', hydroEvent, operationId);
  }

  function hydroDispatch(el, event, operationId) {
    const component = findComponent(el);
    const parentComponent = findComponent(component.element.parentElement);

    if (event.scope === 'parent' && !parentComponent) {
      return;
    }

    const scope = event.scope === 'parent' ? parentComponent.id : 'global';

    const eventName = `${scope}:${event.name}`;
    const eventData = {
      detail: {
        data: event.data,
        subject: event.subject,
        operationId
      }
    };

    document.dispatchEvent(new CustomEvent(eventName, eventData));

    if (event.subject) {
      const subjectEventName = `${scope}:${event.name}:${event.subject}`;
      document.dispatchEvent(new CustomEvent(subjectEventName, eventData));
    }
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
        formData: new FormData(),
        operationId: generateGuid()
      };
    }

    const formData = binding[component.id].formData;
    const bindAlreadyInitialized = [...formData].length !== 0;

    if (!bindAlreadyInitialized) {
      binding[component.id].operationId = generateGuid();
    }

    let propertyName = el.getAttribute('name');

    if (el.tagName === "INPUT" && el.type === 'checkbox') {
      formData.set(propertyName, el.checked);
    } else if (el.tagName === "INPUT" && el.type === 'file') {
      if (el.files.length) {
        Array.from(el.files).forEach((file) => {
          formData.append(propertyName, file);
        });
      } else {
        formData.set(propertyName, '');
      }
    } else {
      formData.set(propertyName, el.value);
    }

    el.setAttribute("data-operation-id", binding[component.id].operationId);
    el.classList.add('hydro-request');

    if (bindAlreadyInitialized) {
      return Promise.resolve();
    }

    if (binding[component.id].timeout) {
      clearTimeout(binding[component.id].timeout);
    }

    binding[component.id].promise = new Promise(resolve => {
      binding[component.id].timeout = setTimeout(async () => {
        const requestFormData = binding[component.id].formData;
        // binding[url].formData = new FormData();
        const bindOperationId = binding[component.id].operationId;
        dirty[propertyName] = bindOperationId;
        await hydroRequest(el, url, { formData: requestFormData }, 'bind', null, bindOperationId);
        if (dirty[propertyName] === bindOperationId) {
          delete dirty[propertyName];
        }
        resolve();
      }, 10);
    });

    return binding[component.id].promise;
  }

  async function hydroAction(el, component, action) {
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
    el.setAttribute("data-operation-id", operationId);

    await hydroRequest(el, url, { parameters: action.parameters }, 'action', null, operationId, true);
  }

  let operationStatus = {};

  async function hydroRequest(el, url, requestData, type, eventData, operationId, morphActiveElement) {
    if (!document.contains(el)) {
      return;
    }

    const component = el.closest("[hydro]");
    const componentId = component.getAttribute("id");
    const componentName = component.getAttribute("hydro-name");

    if (!component) {
      throw new Error('Cannot determine the closest Hydro component');
    }

    const parentComponent = findComponent(component.parentElement);

    let disableTimer;
    let classTimeout;

    if (operationId) {
      if (!operationStatus[operationId]) {
        operationStatus[operationId] = 0;
      }

      if (el.getAttribute('data-operation-id') === operationId) {
        classTimeout = setTimeout(() => el.classList.add('hydro-request'), 200);

        if (type !== 'bind') {
          disableTimer = setTimeout(() => el.disabled = true, 200);
        }
      }

      operationStatus[operationId]++;
    }

    if (type === 'action') {
      if (binding[componentId]?.promise) {
        await binding[componentId].promise;
      }
    }

    await enqueueHydroPromise(componentId, async () => {
      try {
        let headers = {
          'Hydro-Request': 'true'
        };

        if (config.Antiforgery) {
          headers[config.Antiforgery.HeaderName] = config.Antiforgery.Token;
        }

        let requestForm = requestData?.formData || new FormData();

        requestForm.append('__hydro_type', type);

        if (requestData.parameters) {
          const paramsData = JSON.stringify(requestData.parameters, (k, v) => v === undefined ? null : v);
          requestForm.append('__hydro_parameters', paramsData)
        }

        if (requestData.eventData) {
          requestForm.append('__hydro_event', JSON.stringify(requestData.eventData))
        }

        const scripts = component.querySelectorAll('script[data-id]');
        const dataIds = Array.from(scripts).map(script => script.getAttribute('data-id')).filter(d => d !== componentId);
        requestForm.append('__hydro_componentIds', JSON.stringify([componentId, ...dataIds]));

        const scriptTag = component.querySelector(`script[data-id='${componentId}']`);
        requestForm.append('__hydro_model', scriptTag.textContent);

        if (operationId) {
          headers['Hydro-Operation-Id'] = operationId;
        }

        if (type === 'bind') {
          binding[componentId].formData = new FormData();
        }

        const response = await fetch(url, {
          method: 'POST',
          body: requestForm,
          headers: headers
        });

        if (!response.ok) {
          if (response.status === 400 && config.Antiforgery && response.headers.get("Refresh-Antiforgery-Token")) {
            const json = await response.json();
            config.Antiforgery.Token = json.token;
          } else if (response.status === 403) {
            if (type !== 'event') {
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json({ message: "Unauthorized access" }) } }));
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
              document.dispatchEvent(new CustomEvent(`global:UnhandledHydroError`, { detail: { data: toBase64Json(eventDetail) } }));
              document.dispatchEvent(new CustomEvent(`unhandled-hydro-error`, { detail: eventDetail }));
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
                    // hydro component placeholder, so skipping
                    skip();
                    counter++;
                    return;
                  }
                }

                if (from.getAttribute && from.getAttribute("data-operation-id")) {
                  // skip result from bind operation when this operating element is not a hydro component and is awaiting a request already
                  if (type === 'bind' && operationId !== from.getAttribute("data-operation-id") && from.getAttribute("hydro") === null) {
                    skip();
                    counter++;
                    return;
                  }

                  // set the operation id, disabled state and hydro class that would be lost after morph
                  to.setAttribute("data-operation-id", from.getAttribute("data-operation-id"));
                  to.disabled = from.disabled;
                  if (from.classList.contains('hydro-request')) {
                    to.classList.add('hydro-request');
                  }
                }

                const fieldName = from.getAttribute && from.getAttribute("name");

                if (fieldName) {
                  if (dirty[fieldName] && dirty[fieldName] !== operationId) {
                    skip();
                    counter++;
                    return;
                  }
                }

                if (from.tagName === "INPUT" && from.type === 'checkbox') {
                  from.checked = to.checked;
                }

                if (from.tagName === "INPUT" && from.type === 'radio') {
                  from.checked = to.checked;
                }

                if ((from.tagName === "SELECT" || (from.tagName === "INPUT" && ['password', 'text', 'number', 'date', 'email'].includes(from.type))) && from.value !== to.value) {
                  if (document.activeElement === from) {
                    if (morphActiveElement && from.value !== to.value) {
                      from.value = to.value;
                    }
                  } else {
                    from.value = to.value;
                  }
                }

                counter++;
              }
            });

            setTimeout(() => {
              enablePlainScripts(component);
              document.dispatchEvent(new CustomEvent('HydroComponentUpdate', {
                detail: { component: { id: componentId, name: componentName, element: component }, url, type }
              }));
            });
          }

          const scripts = response.headers.get('hydro-js');
          if (scripts) {
            setTimeout(() => appendComponentScripts(component, fromBase64Json(scripts)));
          }

          const locationHeader = response.headers.get('Hydro-Location');
          if (locationHeader) {
            let locationData = JSON.parse(locationHeader);
            await loadPageContent(locationData.path, true, null, locationData.payload);
          }

          const redirectHeader = response.headers.get('Hydro-Redirect');
          if (redirectHeader) {
            window.location.href = redirectHeader;
          }

          const contentDisposition = response.headers.get('Content-Disposition');

          if (contentDisposition) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);

            if (contentDisposition.includes('inline')) {
              window.open(url, '_blank');
            } else if (contentDisposition.includes('attachment')) {
              const fileName = extractFileNameFromContentDisposition(contentDisposition);

              const a = document.createElement('a');
              a.href = url;
              a.download = fileName || 'file';
              document.body.appendChild(a);
              a.click();
              a.remove();
            }
          }

          dispatchEvents(response.headers.get('Hydro-Trigger'), component);
        }
      } catch (error) {
        document.dispatchEvent(new CustomEvent(`unhandled-hydro-error`, { detail: { name: error.name, message: error.message } }));
      } finally {
        setTimeout(() => {
          clearTimeout(classTimeout);
          clearTimeout(disableTimer);

          if (operationId) {
            const operationTrigger = document.querySelectorAll(`[data-operation-id="${operationId}"]`);
            operationStatus[operationId]--;

            if (operationTrigger.length && (operationStatus[operationId] <= 0)) {
              operationTrigger.forEach(trigger => {
                trigger.disabled = false;
                trigger.classList.remove('hydro-request');
                trigger.removeAttribute('data-operation-id');
              })
            }
          }
        }, 20); // make sure it's delayed more than 0 and less than 50
      }
    });
  }

  function dispatchEvents(serializedEvents, componentElement) {
    if (!serializedEvents) {
      return;
    }

    const parentComponent = componentElement ? findComponent(componentElement.parentElement) : null;

    const events = JSON.parse(serializedEvents);
    events.forEach(trigger => {
      if (trigger.scope === 'parent' && !parentComponent) {
        return;
      }

      const eventScope = trigger.scope === 'parent' ? parentComponent.id : 'global';
      const eventName = `${eventScope}:${trigger.name}`;
      const eventData = {
        detail: {
          data: trigger.data,
          subject: trigger.subject,
          operationId: trigger.operationId
        }
      };

      setTimeout(() => {
        document.dispatchEvent(new CustomEvent(eventName, eventData));

        if (trigger.subject) {
          const subjectEventName = `${eventScope}:${trigger.name}:${trigger.subject}`;
          document.dispatchEvent(new CustomEvent(subjectEventName, eventData));
        }
      });
    });
  }

  function isElementDirty(element) {
    const type = element.type;
    if (['checkbox', 'radio'].includes(type)) {
      if (element.checked !== element.defaultChecked) {
        return true;
      }
    } else if (['hidden', 'password', 'text', 'textarea', 'number', 'date', 'email'].includes(type)) {
      if (element.value !== element.defaultValue) {
        return true;
      }
    } else if (['file'].includes(type)) {
      return true;
    } else if (["select-one", "select-multiple"].includes(type)) {
      for (let j = 0; j < element.options.length; j++) {
        if (element.options[j].selected !== element.options[j].defaultSelected) {
          return true;
        }
      }
    }
  }

  function extractFileNameFromContentDisposition(disposition) {
    const props = disposition.split(';').map(prop => prop.trim().split('='));

    const encodedFileName = props.find(prop => prop[0] === 'filename*');

    const encodingMark = "utf-8''";
    if (encodedFileName && encodedFileName[1].toLowerCase().startsWith(encodingMark)) {
      return decodeURIComponent(encodedFileName[1].substring(encodingMark.length));
    }

    const regularFileName = props.find(prop => prop[0] === 'filename');

    if (regularFileName) {
      return regularFileName[1].replace(/['"]/g, '');
    }

    return null;
  }

  function toBase64Json(value) {
    return value !== null && value !== undefined
      ? btoa(String.fromCodePoint(...new TextEncoder().encode(JSON.stringify(value))))
      : null;
  }

  function fromBase64Json(encoded) {
    return encoded !== null && encoded !== undefined
      ? JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(encoded), c => c.charCodeAt(0))))
      : null;
  }

  function waitFor(objectName, maxTime) {
    return new Promise((resolve, reject) => {
      const check = () => {
        if (window[objectName]) {
          clearInterval(interval);
          clearTimeout(timeout);
          resolve(window[objectName]);
        }
      };

      const interval = setInterval(check, 100);

      const timeout = setTimeout(() => {
        clearInterval(interval);
        reject();
      }, maxTime || 10000);

      check();
    });
  }

  window.addEventListener('popstate', async function () {
    if (document.location.pathname + document.location.search === currentPathname) {
      return;
    }

    await loadPageContent(window.location.href, false);
  });

  return {
    hydroEvent,
    hydroBind,
    hydroAction,
    enableHydroScripts,
    loadPageContent,
    findComponent,
    hydroDispatch,
    generateGuid,
    waitFor,
    executeComponentJs,
    dispatchEvents,
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
        el.setAttribute("data-operation-id", operationId);

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

      let timeout;

      const eventHandler = async (event) => {
        if (event === 'submit' || event === 'click') {
          event.preventDefault();
        }

        const target = event.currentTarget;

        if (debounce) {
          clearTimeout(timeout);
          timeout = setTimeout(() => window.Hydro.hydroBind(target), debounce || 0);
        } else {
          await window.Hydro.hydroBind(target);
        }
      };

      el.addEventListener(event, eventHandler);
      cleanup(() => {
        el.removeEventListener(event, eventHandler);
      });
    });
  }).before('on');

  Alpine.directive('hydro-polling', Alpine.skipDuringClone((el, { value, expression, modifiers }, { cleanup }) => {
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

        await window.Hydro.hydroAction(el, component, { name: expression });
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
      const globalEventName = data.subject ? `global:${data.name}:${data.subject}` : `global:${data.name}`;
      const localEventName = data.subject ? `${component.id}:${data.name}:${data.subject}` : `${component.id}:${data.name}`;
      const eventPath = data.path;
      const componentElement = component.element;
      const eventData = el.getAttribute("x-on-hydro-event")

      const eventHandler = async (event) => {
        await window.Hydro.hydroEvent(componentElement, eventData, eventPath, event.detail);
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
          await window.Hydro.loadPageContent(url, true, () => currentBoostUrl === url);
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

  Alpine.directive('hydro-focus', (el, { expression }, { effect }) => {
    effect(() => {
      el.querySelector(expression || 'input').focus();
    });
  });

  Alpine.data('hydro', () => {
    let debounceArray = {};

    return ({
      $component: null,
      init() {
        const component = window.Hydro.findComponent(this.$el);
        this.$component = component;

        window.Hydro.enableHydroScripts(this.$el);
        document.dispatchEvent(new CustomEvent('HydroComponentInit', {
          detail: { component: component }
        }));
      },
      async invoke(e, action) {
        if (["click", "submit"].includes(e.type) && ['A', 'BUTTON', 'FORM'].includes(this.$el.tagName)) {
          e.preventDefault();
        }
        await window.Hydro.hydroAction(this.$el, this.$component, action);
      },
      async dispatch(e, event) {
        const el = this.$el;
        const operationId = window.Hydro.generateGuid();
        el.setAttribute("data-operation-id", operationId);
        window.Hydro.hydroDispatch(el, event, operationId);
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
