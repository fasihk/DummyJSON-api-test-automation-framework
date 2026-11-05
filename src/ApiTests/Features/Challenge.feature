Feature: DummyJSON functional system test challenge
  As an API test author
  I want to validate the documented Auth, Users, Products, and Carts endpoints
  So that their status codes, response payloads, and data attributes are verified

  Background:
    Given DummyJSON is available

  @challenge @auth
  Scenario: Authenticate and store the access token
    When I authenticate with username "emilys" and password "emilyspass"
    Then the response status code should be 200
    And response JSON path "accessToken" should exist
    And the access token should be stored for this scenario

  @challenge @auth
  Scenario: Validate the current authenticated user
    When I authenticate with username "emilys" and password "emilyspass"
    And I request the current authenticated user
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "firstName" should equal "Emily"
    And response JSON path "lastName" should equal "Johnson"

  @challenge @users
  Scenario: Count users with red hair
    When I send a GET request to "/users/filter?key=hair.color&value=Red"
    Then the response status code should be 200
    And response JSON path "total" should equal "25"
    And response JSON path "users" should contain 25 items
    And every item in response JSON array "users" should have "hair.color" equal to "Red"

  @challenge @users
  Scenario: Add a user with a complete address
    When I send a POST request to "/users/add" with JSON body:
      """
      {
        "firstName": "Jordan",
        "lastName": "Morgan",
        "age": 31,
        "gender": "female",
        "hair": {
          "color": "Red",
          "type": "Wavy"
        },
        "address": {
          "address": "123 Test Street",
          "city": "Austin",
          "state": "Texas",
          "stateCode": "TX",
          "postalCode": "78701",
          "coordinates": {
            "lat": 30.2672,
            "lng": -97.7431
          },
          "country": "United States"
        }
      }
      """
    Then the response status code should be 201
    And response JSON path "id" should be greater than 0
    And response JSON path "firstName" should equal "Jordan"
    And response JSON path "lastName" should equal "Morgan"
    And response JSON path "age" should equal "31"
    And response JSON path "gender" should equal "female"
    And response JSON path "hair.color" should equal "Red"
    And response JSON path "hair.type" should equal "Wavy"
    And response JSON path "address.address" should equal "123 Test Street"
    And response JSON path "address.city" should equal "Austin"
    And response JSON path "address.state" should equal "Texas"
    And response JSON path "address.stateCode" should equal "TX"
    And response JSON path "address.postalCode" should equal "78701"
    And response JSON path "address.coordinates.lat" should equal "30.2672"
    And response JSON path "address.coordinates.lng" should equal "-97.7431"
    And response JSON path "address.country" should equal "United States"

  @challenge @products
  Scenario: Validate the iPhone 13 Pro price
    When I send a GET request to "/products/search?q=iPhone%2013%20Pro"
    Then the response status code should be 200
    And response JSON path "total" should equal "1"
    And response JSON path "products[0].id" should equal "123"
    And response JSON path "products[0].title" should equal "iPhone 13 Pro"
    And response JSON path "products[0].price" should equal "1099.99"

  @challenge @products
  Scenario: Retrieve only the 15th product using pagination
    When I send a GET request to "/products?limit=1&skip=14&select=id,title,price"
    Then the response status code should be 200
    And response JSON path "skip" should equal "14"
    And response JSON path "limit" should equal "1"
    And response JSON path "products" should contain 1 item
    And response JSON path "products[0].id" should equal "15"
    And response JSON path "products[0].title" should equal "Wooden Bathroom Sink With Mirror"
    And response JSON path "products[0].price" should equal "799.99"

  @challenge @products
  Scenario: Update the title of product 121
    When I send a PATCH request to "/products/121" with JSON body:
      """
      {
        "title": "Challenge Updated Product 121"
      }
      """
    Then the response status code should be 200
    And response JSON path "id" should equal "121"
    And response JSON path "title" should equal "Challenge Updated Product 121"

  @challenge @carts
  Scenario: Add five products to a cart and validate total discount percentage
    When I send a POST request to "/carts/add" with JSON body:
      """
      {
        "userId": 1,
        "products": [
          { "id": 1, "quantity": 1 },
          { "id": 2, "quantity": 1 },
          { "id": 3, "quantity": 1 },
          { "id": 4, "quantity": 1 },
          { "id": 5, "quantity": 1 }
        ]
      }
      """
    Then the response status code should be 201
    And response JSON path "totalProducts" should equal "5"
    And response JSON path "products" should contain 5 items
    And the cart discount percentage calculated from each item's prices should equal "13.37"

  @challenge @carts
  Scenario: Delete a cart
    When I send a DELETE request to "/carts/1"
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "isDeleted" should equal "true"
    And response JSON path "deletedOn" should exist