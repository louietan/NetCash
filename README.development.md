This document is for keeping notes related to the development of this
project, including how GnuCash works under the hood.

# GnuCash

## Multi-concurrency transactions

This section explains `value`, `amount` on splits and `currency` on transactions.

In the design of GnuCash:
- A transaction consists of `currency` and 2 or more splits.
- A split consists of `value`, `amount` and `account`.
- An account is assigned with a currency (or commodity).

Let's say we use 10000 CN¥ to buy Bitcoins and Ethereum coins.

The journal looks like this:

| Account | Debit | Credit |
| ------- | ----: | -----: |
| Assets:Bitcoin | BTC 0.034 ||
| Assets:Ethereum | ETH 0.23 ||
| Assets:Checking || CN¥ 10000 |

Now to explain the terminology:

- `amount` (or `quantity` or `qty` for short) is the number of the
  account-currency involved. (10000 CN¥, 0.034 BTC, 0.23 ETH in this case)

- `value` is actually also "amount", it's the amount of the transaction-currency involved.

> So what's the purpose of the transaction-currency?

When you enter the above journal to GnuCash, GnuCash has to ensure the
transaction is balanced, right? i.e. the equation `0 = BTC 0.034 + ETH
0.23 - CN¥ 10000` be true.

But BTC, ETH and CN¥ are 3 different things and can not be
added/substracted with each other. It's like to solve `1 inch + 1
centimeter = ?`, we need a "common unit", which could be inch: `1 inch + x inch =
y inch` or centimeter: `x centimeter + 1 centimeter = y centimeter`.

It's the purpose of the transaction-currency, it serves as the "common
currency" or "valuation currency", all the splits are valuated against
this currency to determine if the transaction is balanced.

In GnuCash GUI, for each split you either explicitly enter the value
or enter the share price (so that `value = amount x price`).

# NetCash Web

## MobX

MobX was chosen as the state management library because it works like a magic!

But for it's magic to work with React, sometimes you have to pay a
little extra attention.

The most common one is to wrap those components you want them to be reactive to stores as `observer`s,
**including child and callback components**.

Example taken from MobX docs site.
```js
class Todo {
    title = "test"
    done = true

    constructor() {
        makeAutoObservable(this)
    }
}

const TodoView = observer(({ todo }: { todo: Todo }) =>
   // WRONG: GridRow won't pick up changes in todo.title / todo.done
   //        since it isn't an observer.
   return <GridRow data={todo} />

   // CORRECT: let `TodoView` detect relevant changes in `todo`,
   //          and pass plain data down.
   return <GridRow data={{
       title: todo.title,
       done: todo.done
   }} />

   // CORRECT: using `toJS` works as well, but being explicit is typically better.
   return <GridRow data={toJS(todo)} />
)


const TodoView = observer(({ todo }: { todo: Todo }) => {
    // WRONG: GridRow.onRender won't pick up changes in todo.title / todo.done
    //        since it isn't an observer.
    return <GridRow onRender={() => <td>{todo.title}</td>} />

    // CORRECT: wrap the callback rendering in Observer to be able to detect changes.
    return <GridRow onRender={() => <Observer>{() => <td>{todo.title}</td>}</Observer>} />
})
```
